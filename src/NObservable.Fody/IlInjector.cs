using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using ExceptionHandler = Mono.Cecil.Cil.ExceptionHandler;
using OpCode = Mono.Cecil.Cil.OpCode;
using OpCodes = Mono.Cecil.Cil.OpCodes;

namespace NObservable.Fody
{
    public static class IlInjector
    {
        private const string TrackerFieldName = "NObservable_2aa5c83d645c4ab18e9c22a3ebba7960";
        private const string BlazorHelperFieldName = "NObservableBlazorHelper_565c2f9b447043faaf97a44c9cc4fcdd";

        static FieldReference FindTrackerField(TypeDefinition type, string fieldName)
        {
            if (type == null)
                return null;
            var found = type.Fields.FirstOrDefault(f => f.Name == fieldName);
            if (found != null)
                return found;
            return FindTrackerField(type.BaseType?.Resolve(), fieldName);
        }
        
        static FieldReference FindOrInjectField(WeavingContext context, TypeDefinition type,
            string fieldName, TypeReference fieldType, Action<Helper, FieldDefinition> builder)
        
        {
            var found = FindTrackerField(type, fieldName);
            if (found != null)
                return found;
            var field = new FieldDefinition(fieldName, FieldAttributes.FamANDAssem, fieldType);
            type.Fields.Add(field);
            foreach (var ctor in type.GetConstructors())
            {
                using (var il = new Helper(ctor))
                {
                    builder(il, field);
                }
            }
            return field;
        }

        static FieldReference FindOrInjectTrackerField(WeavingContext context, TypeDefinition type)
        {
            return FindOrInjectField(context, type, TrackerFieldName, context.PropertyTrackerReference, (il, field) =>
            {
                var first = il.Body.Instructions.First();
                il.AddBefore(first, new Instructions
                {
                    OpCodes.Ldarg_0,
                    {OpCodes.Ldflda, field},
                    {OpCodes.Call, context.PropertyTrackerInitReference}

                });
            });
        }

        static FieldReference FindOrInjectBlazorHelperField(WeavingContext context, TypeDefinition type)
        {
            return FindOrInjectField(context, type, BlazorHelperFieldName, context.BlazorComponentHelperReference,
                (il, field) =>
                {
                    il.AddBefore(il.Body.Instructions.First(), new Instructions
                    {
                        OpCodes.Ldarg_0,
                        {OpCodes.Ldfld, field},
                        {OpCodes.Brtrue, il.Body.Instructions.First()},
                        OpCodes.Ldarg_0,
                        OpCodes.Ldarg_0,
                        {OpCodes.Newobj, context.BlazorComponentHelperCtorReference},
                        {OpCodes.Stfld, field}
                    });
                });
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

            using (var getterIl = new Helper(getter))
            {
                var getterFirst = getter.Body.Instructions.First();
                getterIl.AddBefore(getterFirst, new Instructions
                {
                    OpCodes.Ldarg_0,
                    {OpCodes.Ldflda, trackerField},
                    {OpCodes.Ldc_I4, propId},
                    {OpCodes.Call, context.PropertyTrackerTrackGetReference}

                });
            }

            using (var setterIl = new Helper(setter))
            {
                var oldValueVariable = new VariableDefinition(prop.PropertyType);
                var oldValueIsSet = new VariableDefinition(context.TypeSystem.BooleanReference);
                setter.Body.Variables.Add(oldValueVariable);
                setter.Body.Variables.Add(oldValueIsSet);


                var originalSetterFirst = setter.Body.Instructions.First();

                // Prologue


                setterIl.AddBefore(originalSetterFirst, new Instructions
                {
                    // var oldValueIsSet = false
                    {OpCodes.Ldc_I4_0},
                    {OpCodes.Stloc, oldValueIsSet},

                    // this._tracker.EnterTrackSet();
                    {OpCodes.Ldarg_0},
                    {OpCodes.Ldflda, trackerField},
                    {OpCodes.Call, context.PropertyTrackerEnterTrackSetReference},

                    // var old = this.Property;
                    {OpCodes.Ldarg_0},
                    {OpCodes.Callvirt, getter},
                    {OpCodes.Stloc, oldValueVariable},

                    // oldValueIsSet = true
                    {OpCodes.Ldc_I4_1},
                    {OpCodes.Stloc, oldValueIsSet}
                });


                // New final return instruction

                var lastRet = Instruction.Create(OpCodes.Ret);
                setterIl.Append(lastRet);

                var leaveTry = Instruction.Create(OpCodes.Leave_S, lastRet);
                if (lastRet.Previous.OpCode == OpCodes.Ret)
                    setterIl.Replace(lastRet.Previous, leaveTry);
                else
                    setterIl.AddBefore(lastRet, leaveTry);


                var genericTrackSet = new GenericInstanceMethod(context.PropertyTrackerTrackSetReference)
                {
                    GenericArguments = {prop.PropertyType},


                };


                //var leaveFinally = setterIl.Create(OpCodes.Leave_S, lastRet);
                var leaveFinally = Instruction.Create(OpCodes.Endfinally);


                var fastFinallyInstructions = new Instructions
                {
                    OpCodes.Ldarg_0,
                    {OpCodes.Ldflda, trackerField},
                    {OpCodes.Call, context.PropertyTrackerLeaveTrackSetReference}
                };

                var finallyInstructions = new Instructions
                {
                    // if(!oldValueIsSet) goto: fastFinally
                    {OpCodes.Ldloc, oldValueIsSet},
                    {OpCodes.Brfalse, fastFinallyInstructions.First()},

                    // this._tracker.TrackSet({id}, oldValue, Prop ({this._tracker.LeaveTrackSet()}))
                    {OpCodes.Ldarg_0},
                    {OpCodes.Ldflda, trackerField},

                    {OpCodes.Ldc_I4, propId},

                    {OpCodes.Ldloc, oldValueVariable},

                    {OpCodes.Ldarg_0},
                    {OpCodes.Callvirt, getter},

                    // this._tracker.LeaveTrackSet()
                    {OpCodes.Ldarg_0},
                    {OpCodes.Ldflda, trackerField},
                    {OpCodes.Call, context.PropertyTrackerLeaveTrackSetReference},

                    {OpCodes.Call, genericTrackSet},
                    {OpCodes.Br, leaveFinally}
                };


                setterIl.AddBefore(lastRet, finallyInstructions);
                setterIl.AddBefore(lastRet, fastFinallyInstructions);
                setterIl.AddBefore(lastRet, leaveFinally);


                foreach (var i in setter.Body.Instructions.Where(x => x.OpCode == OpCodes.Ret && x != lastRet).ToList())
                {
                    setterIl.Replace(i, Instruction.Create(OpCodes.Leave_S, lastRet));
                }

                setter.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
                {
                    TryStart = setter.Body.Instructions.First(),
                    TryEnd = leaveTry.Next,
                    HandlerStart = leaveTry.Next,
                    HandlerEnd = leaveFinally.Next
                });
                setter.Body.MaxStackSize += 2;
            }
        }

        public static void InstrumentBlazorComponent(WeavingContext context, TypeDefinition type)
        {
            var field = FindOrInjectBlazorHelperField(context, type);
            var method = type.Methods.First(m => m.Name == "BuildRenderTree");
            using (var il = new Helper(method))
            {
                var originalFirst = il.Body.Instructions.First();

                il.AddBefore(originalFirst, new Instructions()
                {
                    OpCodes.Ldarg_0,
                    {OpCodes.Ldfld, field},
                    {OpCodes.Callvirt, context.BlazorComponentHelperOnRenderEnterReference}
                });
                
                
                var lastRet = Instruction.Create(OpCodes.Ret);
                il.Append(lastRet);

                var leaveTry = Instruction.Create(OpCodes.Leave_S, lastRet);
                if (lastRet.Previous.OpCode == OpCodes.Ret)
                    il.Replace(lastRet.Previous, leaveTry);
                else
                    il.AddBefore(lastRet, leaveTry);
                foreach (var i in method.Body.Instructions.Where(x => x.OpCode == OpCodes.Ret && x != lastRet).ToList())
                {
                    il.Replace(i, Instruction.Create(OpCodes.Leave_S, lastRet));
                }

                var leaveFinally = Instruction.Create(OpCodes.Endfinally);
                il.AddBefore(lastRet, leaveFinally);
                
                
                il.AddBefore(leaveFinally, new Instructions()
                {
                    OpCodes.Ldarg_0,
                    {OpCodes.Ldfld, field},
                    {OpCodes.Callvirt, context.BlazorComponentHelperOnRenderLeaveReference}
                });
                
                il.Body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
                {
                    TryStart = originalFirst,
                    TryEnd = leaveTry.Next,
                    HandlerStart = leaveTry.Next,
                    HandlerEnd = leaveFinally.Next
                });
                
            }

            if (!(type.Methods.Any(m => m.Name == "ShouldRender" && m.DeclaringType == type)))
            {
                var blazorComponent = type.FindBaseType(t =>
                    t.FullName == "Microsoft.AspNetCore.Blazor.Components.BlazorComponent");
                if (blazorComponent != null)
                {
                    var baseMethod = blazorComponent.Methods.First(m =>
                        m.Name == "ShouldRender" && m.Attributes.HasFlag(MethodAttributes.Virtual));
                    var shouldRender = CreateMethodOverride(type, baseMethod);
                    using (var il = new Helper(shouldRender))
                    {
                        il.Append(new Instructions
                        {
                            OpCodes.Ldarg_0,
                            {OpCodes.Ldfld, field},
                            {OpCodes.Callvirt, context.BlazorComponentHelperShouldRenderReference},
                            OpCodes.Ret
                        });
                    }
                    
                }
            }
        }

        static TypeDefinition FindBaseType(this TypeDefinition def, Func<TypeDefinition, bool> condition)
        {
            var b = def?.BaseType.Resolve();
            if (b == null)
                return null;
            if (condition(b))
                return b;
            return FindBaseType(b, condition);
        }
        
        
        static MethodDefinition CreateMethodOverride(TypeDefinition targetType, MethodDefinition baseMethod)
        {
            MethodDefinition newMethod = new MethodDefinition(baseMethod.Name,
                baseMethod.Attributes & ~MethodAttributes.NewSlot,
                baseMethod.ReturnType);
            newMethod.Name = baseMethod.Name;
            if (baseMethod.HasParameters)
                throw new NotImplementedException("TODO: Methods with parameters aren't supported yet");

            newMethod.ImplAttributes = baseMethod.ImplAttributes;
            newMethod.SemanticsAttributes = baseMethod.SemanticsAttributes;
            targetType.Methods.Add(newMethod);
            return newMethod;
        }


        class Helper : IDisposable
        {
            private readonly MethodDefinition _method;
            private ILProcessor _processor;
            
            public Helper(MethodDefinition method)
            {
                _method = method;
                _processor = method.Body.GetILProcessor();
            }

            public MethodBody Body => _method.Body;
            
            public Helper AddBefore(Instruction before, params Instruction[] instructions)
                => AddBefore(before, (IEnumerable<Instruction>) instructions);
            
            public Helper AddBefore(Instruction before, IEnumerable<Instruction> instructions)
            {
                foreach (var i in instructions)
                    _processor.InsertBefore(before, i);
                
                return this;
            }

            public void Append(params Instruction[] instructions) => Append((IEnumerable<Instruction>) instructions);
            public void Append(IEnumerable<Instruction> instructions)
            {
                foreach (var i in instructions)
                    _processor.Append(i);
            }

            public void Replace(Instruction what, Instruction with)
            {
                _processor.Replace(what, with);
            }

            public void Dispose() => _method.UpdateDebugInfo();
        }

        class Instructions : List<Instruction>
        {

            public void Add(OpCode opcode) => Add(Instruction.Create(opcode));
            public void Add(OpCode opcode, int op) => Add(Instruction.Create(opcode, op));
            public void Add(OpCode opcode, MethodReference op) => Add(Instruction.Create(opcode, op));
            public void Add(OpCode opcode, VariableDefinition op) => Add(Instruction.Create(opcode, op));
            public void Add(OpCode opcode, FieldReference op) => Add(Instruction.Create(opcode, op));
            public void Add(OpCode opcode, Instruction op) => Add(Instruction.Create(opcode, op));
        }

    }
}