using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using ExceptionHandler = Mono.Cecil.Cil.ExceptionHandler;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace NObservable.Fody
{
    public static class IlInjector
    {
        private const string FieldName = "NObservable_2aa5c83d645c4ab18e9c22a3ebba7960";

        static FieldReference FindTrackerField(TypeDefinition type)
        {
            if (type == null)
                return null;
            var found = type.Fields.FirstOrDefault(f => f.Name == FieldName);
            if (found != null)
                return found;
            return FindTrackerField(type.BaseType?.Resolve());
        }
        
        public static FieldReference FindOrInjectTrackerField(WeavingContext context, TypeDefinition type)
        {
            var found = FindTrackerField(type);
            if (found != null)
                return found;
            var field = new FieldDefinition(FieldName, FieldAttributes.FamANDAssem,
                context.PropertyTrackerReference);
            type.Fields.Add(field);
            foreach (var ctor in type.GetConstructors())
            {
                var il = ctor.Body.GetILProcessor();
                var first = il.Body.Instructions.First();
                il.InsertBefore(first,
                    il.Create(OpCodes.Ldarg_0),
                    il.Create(OpCodes.Ldflda, field),
                    il.Create(OpCodes.Call, context.PropertyTrackerInitReference));
            }
            return field;
        }

        static int FindBaseTypeObservablePropertyCount(TypeDefinition typeDef)
        {
            if (typeDef == null)
                return 0;
            var attr = typeDef.CustomAttributes.FirstOrDefault(a =>
                a.AttributeType.Name == "NObservablePropertyCountAttribute");
            if (attr != null)
            {
                return (int) attr.ConstructorArguments.First().Value;
            }
            else
                return FindBaseTypeObservablePropertyCount(typeDef.BaseType?.Resolve());
        }

        static int SetNextPropertyId(WeavingContext context, TypeDefinition typeDef)
        {
            var existing =
                typeDef.CustomAttributes.FirstOrDefault(a =>
                    a.AttributeType.AreEqual(context.PropertyCountAttributeReference));
            if (existing != null)
            {
                var nextId = (int) existing.ConstructorArguments[0].Value + 1;
                existing.ConstructorArguments[0] = new CustomAttributeArgument(existing.AttributeType, nextId);
                return nextId;
            }
            else
            {
                var nextId = FindBaseTypeObservablePropertyCount(typeDef) + 1;
                typeDef.CustomAttributes.Add(new CustomAttribute(context.PropertyCountAttributeCtorReference)
                {
                    ConstructorArguments =
                        {new CustomAttributeArgument(context.TypeSystem.Int32Reference, nextId)}
                });
                return nextId;
            }
        }

        
            
        public static void InstrumentProperty(WeavingContext context, PropertyDefinition prop)
        {
            var getter = prop.GetMethod;
            var setter = prop.SetMethod;
            var propId = SetNextPropertyId(context, getter.DeclaringType);
            var trackerField = FindOrInjectTrackerField(context, getter.DeclaringType);
            
            var getterIl = getter.Body.GetILProcessor();
            var getterFirst = getterIl.Body.Instructions.First();

           
            getterIl.InsertBefore(getterFirst,
                //this._tracker.TrackGet(const propId)
                getterIl.Create(OpCodes.Ldarg_0),
                getterIl.Create(OpCodes.Ldflda, trackerField),
                getterIl.Create(OpCodes.Ldc_I4, propId),
                getterIl.Create(OpCodes.Call, context.PropertyTrackerTrackGetReference)
            );
            
            var setterIl = setter.Body.GetILProcessor();
            var oldValueVariable = new VariableDefinition(prop.PropertyType);
            var oldValueIsSet = new VariableDefinition(context.TypeSystem.BooleanReference);
            setter.Body.Variables.Add(oldValueVariable);
            setter.Body.Variables.Add(oldValueIsSet);
            
            var originalSetterFirst = setterIl.Body.Instructions.First();
            
            // Prologue


            setterIl.InsertBefore(originalSetterFirst,
                // var oldValueIsSet = false
                setterIl.Create(OpCodes.Ldc_I4_0),
                setterIl.Create(OpCodes.Stloc, oldValueIsSet),

                // this._tracker.EnterTrackSet();
                setterIl.Create(OpCodes.Ldarg_0),
                setterIl.Create(OpCodes.Ldflda, trackerField),
                setterIl.Create(OpCodes.Call, context.PropertyTrackerEnterTrackSetReference),

                // var old = this.Property;
                setterIl.Create(OpCodes.Ldarg_0),
                setterIl.Create(OpCodes.Callvirt, getter),
                setterIl.Create(OpCodes.Stloc, oldValueVariable),

                // oldValueIsSet = true
                setterIl.Create(OpCodes.Ldc_I4_1),
                setterIl.Create(OpCodes.Stloc, oldValueIsSet)
            );
                

            // New final return instruction
            
            var lastRet = setterIl.Create(OpCodes.Ret);
            setterIl.Append(lastRet);

            var leaveTry = setterIl.Create(OpCodes.Leave_S, lastRet);
            if (lastRet.Previous.OpCode == OpCodes.Ret)
                setterIl.Replace(lastRet.Previous, leaveTry);
            else
                setterIl.InsertBefore(lastRet, leaveTry);
            

            var genericTrackSet = new GenericInstanceMethod(context.PropertyTrackerTrackSetReference)
            {
                GenericArguments = {prop.PropertyType},
                
                
            };


            //var leaveFinally = setterIl.Create(OpCodes.Leave_S, lastRet);
            var leaveFinally = setterIl.Create(OpCodes.Endfinally);
            
            
            var fastFinallyInstructions = new[]
            {
                setterIl.Create(OpCodes.Ldarg_0),
                setterIl.Create(OpCodes.Ldflda, trackerField),
                setterIl.Create(OpCodes.Call, context.PropertyTrackerLeaveTrackSetReference)
            };

            var finallyInstructions = new[]
            {
                // if(!oldValueIsSet) goto: fastFinally
                setterIl.Create(OpCodes.Ldloc, oldValueIsSet),
                setterIl.Create(OpCodes.Brfalse, fastFinallyInstructions.First()),
                
                // this._tracker.TrackSet({id}, oldValue, Prop ({this._tracker.LeaveTrackSet()}))
                setterIl.Create(OpCodes.Ldarg_0),
                setterIl.Create(OpCodes.Ldflda, trackerField),

                setterIl.Create(OpCodes.Ldc_I4, propId),

                setterIl.Create(OpCodes.Ldloc, oldValueVariable),

                setterIl.Create(OpCodes.Ldarg_0),
                setterIl.Create(OpCodes.Callvirt, getter),

                // this._tracker.LeaveTrackSet()
                setterIl.Create(OpCodes.Ldarg_0),
                setterIl.Create(OpCodes.Ldflda, trackerField),
                setterIl.Create(OpCodes.Call, context.PropertyTrackerLeaveTrackSetReference),

                setterIl.Create(OpCodes.Call, genericTrackSet),
                setterIl.Create(OpCodes.Br, leaveFinally)
            };
            

            setterIl.InsertBefore(lastRet, finallyInstructions);
            setterIl.InsertBefore(lastRet, fastFinallyInstructions);
            setterIl.InsertBefore(lastRet, leaveFinally);


            foreach (var i in setter.Body.Instructions.Where(x => x.OpCode == OpCodes.Ret && x != lastRet).ToList())
            {
                setterIl.Replace(i, setterIl.Create(OpCodes.Leave_S, lastRet));
            }

            setter.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = setterIl.Body.Instructions.First(),
                TryEnd = leaveTry.Next,
                HandlerStart = leaveTry.Next,
                HandlerEnd = leaveFinally.Next
            });
            setter.Body.MaxStackSize += 2;
        }

        static ILProcessor InsertBefore(this ILProcessor processor, Instruction before,
            params Instruction[] instructions)
        {
            foreach (var i in instructions)
                processor.InsertBefore(before, i);
            return processor;
        }
    }
}