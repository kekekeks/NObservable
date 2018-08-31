using System;
using System.Linq;
using Mono.Cecil;
using TypeSystem = Fody.TypeSystem;

namespace NObservable.Fody
{
    public  class WeavingContext
    {
        public TypeSystem TypeSystem { get; }
        public ModuleDefinition ModuleDefinition { get; }
        public AssemblyDefinition NObservableAsm { get; }
        
        public TypeReference ObservableAttributeReference { get; }
        public TypeReference PropertyTrackerReference { get; }
        public TypeReference PropertyCountAttributeReference { get; }
        public MethodReference PropertyCountAttributeCtorReference { get; }
        public MethodReference PropertyTrackerInitReference { get; }        
        public MethodReference PropertyTrackerTrackGetReference { get; }        
        public MethodReference PropertyTrackerTrackSetReference { get; }        
        public MethodReference PropertyTrackerEnterTrackSetReference { get; }        
        public MethodReference PropertyTrackerLeaveTrackSetReference { get; }        
        
        public WeavingContext(TypeSystem typeSystem, ModuleDefinition moduleDefinition)
        {
            TypeSystem = typeSystem;
            ModuleDefinition = moduleDefinition;
            var nobservableRef = ModuleDefinition.AssemblyReferences.FirstOrDefault(asm => asm.Name == "NObservable");
            if (nobservableRef == null)
                throw new Exception("Reference to NObervable not found");
            NObservableAsm = ModuleDefinition.AssemblyResolver.Resolve(nobservableRef);

            PropertyTrackerReference = ImportType("NObservable.Internals.PropertyTracker");
            PropertyTrackerInitReference = ImportMethod("NObservable.Internals.PropertyTracker", "Init");
            PropertyTrackerTrackGetReference = ImportMethod("NObservable.Internals.PropertyTracker", "TrackGet");
            PropertyTrackerTrackSetReference = ImportMethod("NObservable.Internals.PropertyTracker", "TrackSet");
            PropertyTrackerEnterTrackSetReference = ImportMethod("NObservable.Internals.PropertyTracker", "EnterTrackSet");
            PropertyTrackerLeaveTrackSetReference = ImportMethod("NObservable.Internals.PropertyTracker", "LeaveTrackSet");
            ObservableAttributeReference = ImportType("NObservable.ObservableAttribute");
            PropertyCountAttributeReference = ImportType("NObservable.Internals.NObservablePropertyCountAttribute");
            PropertyCountAttributeCtorReference =
                ImportMethod("NObservable.Internals.NObservablePropertyCountAttribute", ".ctor");
        }

        public TypeReference ImportType(string name)
        {
            var type = NObservableAsm.MainModule.GetType(name);
            if (type == null)
                throw new Exception($"{name} not found");
            return ModuleDefinition.ImportReference(type);
        }

        public MethodReference ImportMethod(string typeName, string methodName)
        {
            var type = NObservableAsm.MainModule.GetType(typeName);
            if (type == null)
                throw new Exception($"{typeName} not found");
            var method = type.Methods.FirstOrDefault(m => m.Name == methodName);
            if (method == null)
                throw new Exception($"{typeName}::{methodName} not found");
            return ModuleDefinition.ImportReference(method);
        }
    }
}