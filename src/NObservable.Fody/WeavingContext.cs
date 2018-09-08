using System;
using System.Linq;
using Mono.Cecil;
using TypeSystem = Fody.TypeSystem;

namespace NObservable.Fody
{
    public  class WeavingContext
    {
        private readonly ModuleWeaver _weaver;
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
        
        
        public AssemblyDefinition BlazorHelperAsm { get; }
        public TypeReference BlazorComponentHelperReference { get; }
        public MethodReference BlazorComponentHelperCtorReference { get; }
        public MethodReference BlazorComponentHelperOnRenderEnterReference { get; }
        public MethodReference BlazorComponentHelperOnRenderLeaveReference { get; }
        public MethodReference BlazorComponentHelperShouldRenderReference { get; }
        public MethodReference BlazorComponentHelperOnParametersSetReference { get; }
        
        public WeavingContext(TypeSystem typeSystem, ModuleWeaver weaver, ModuleDefinition moduleDefinition)
        {
            _weaver = weaver;
            TypeSystem = typeSystem;
            ModuleDefinition = moduleDefinition;
            
            var blazorRef = moduleDefinition.AssemblyReferences.FirstOrDefault(asm => asm.Name == "NObservable.Blazor");
            if (blazorRef != null)
            {
                var helperType = "NObservable.Blazor.Internals.NObservableBlazorComponentHelper";
                BlazorHelperAsm = ModuleDefinition.AssemblyResolver.Resolve(blazorRef);
                BlazorComponentHelperReference = ImportType(BlazorHelperAsm, helperType);
                BlazorComponentHelperCtorReference =
                    ImportMethod(BlazorHelperAsm, helperType, ".ctor");
                BlazorComponentHelperOnRenderEnterReference =
                    ImportMethod(BlazorHelperAsm, helperType, "OnRenderEnter");
                BlazorComponentHelperOnRenderLeaveReference =
                    ImportMethod(BlazorHelperAsm, helperType, "OnRenderLeave");
                BlazorComponentHelperShouldRenderReference =
                    ImportMethod(BlazorHelperAsm, helperType, "ShouldRender");
                BlazorComponentHelperOnParametersSetReference =
                    ImportMethod(BlazorHelperAsm, helperType, "OnParametersSet");
            }


            var nobservableRef = ModuleDefinition.AssemblyReferences.FirstOrDefault(asm => asm.Name == "NObservable")
                                 ?? BlazorHelperAsm?.MainModule.AssemblyReferences.FirstOrDefault(asm =>
                                     asm.Name == "NObservable");
            if (nobservableRef == null)
                Abort("Reference to NObservable not found");
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

        void Abort(string message)
        {
            _weaver.LogError(message);
            throw new Exception(message);
        }

        public TypeReference ImportType(string name) => ImportType(NObservableAsm, name);
        public TypeReference ImportType(AssemblyDefinition asm, string name)
        {
            var type = asm.MainModule.GetType(name);
            if (type == null)
            {
                Abort($"{name} not found");
            }

            return ModuleDefinition.ImportReference(type);
        }

        public MethodReference ImportMethod(string typeName, string methodName) =>
            ImportMethod(NObservableAsm, typeName, methodName);
        
        public MethodReference ImportMethod(AssemblyDefinition asm, string typeName, string methodName)
        {
            var type = asm.MainModule.GetType(typeName);
            if (type == null)
                Abort($"{typeName} not found");
            var method = type.Methods.FirstOrDefault(m => m.Name == methodName);
            if (method == null)
                Abort($"{typeName}::{methodName} not found");
            return ModuleDefinition.ImportReference(method);
        }
    }
}