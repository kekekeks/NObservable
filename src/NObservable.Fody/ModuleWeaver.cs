using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Fody;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using NObservable.Fody;


// ReSharper disable once CheckNamespace
public class ModuleWeaver : BaseModuleWeaver
{
    IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> col)
    {
        foreach (var t in col)
        {
            yield return t;
            if(t.HasNestedTypes)
                foreach (var nested in AllTypes(t.NestedTypes))
                    yield return nested;
        }
    }

    bool IsBlazorComponent(TypeDefinition type)
    {
        if (type == null)
            return false;
        if (type.FullName == "Microsoft.AspNetCore.Blazor.Components.BlazorComponent")
            return true;
        return IsBlazorComponent(type.BaseType?.Resolve());
    }

    public override void Execute()
    {
        if (Environment.GetEnvironmentVariable("NOBSERVABLE_DEBUG_ATTACH") == "1")
        {
            LogError($"Attach debugger to {Process.GetCurrentProcess().Id}");
            while (!Debugger.IsAttached)
            {
                Thread.Sleep(1000);
            }
        }

        var context = new WeavingContext(TypeSystem, this, ModuleDefinition);
        foreach (var t in AllTypes(ModuleDefinition.Types))
        {
            var observeAll = t.CustomAttributes.Any(ca => ca.AttributeType.AreEqual(context.ObservableAttributeReference));
            foreach (var p in t.Properties)
            {
                var getter = p.GetMethod;
                var setter = p.SetMethod;
                if (getter == null || setter == null)
                    continue;
                if (getter.IsStatic || setter.IsStatic)
                    continue;
                if (observeAll ||
                    p.CustomAttributes.Any(ca => ca.AttributeType.AreEqual(context.ObservableAttributeReference)))
                    IlInjector.InstrumentProperty(context, p);
            }

            if (context.BlazorHelperAsm != null && IsBlazorComponent(t))
            {
                if (t.Interfaces.Any(iface => iface.InterfaceType.FullName == "NObservable.Blazor.IObserverComponent"))
                    IlInjector.InstrumentBlazorComponent(context, t);
            }
        }
    }
    
    public override IEnumerable<string> GetAssembliesForScanning()
    {
        return new[] {"mscorlib", "netstandard", "NObservable", "NObservable.Blazor"};
    }
}
