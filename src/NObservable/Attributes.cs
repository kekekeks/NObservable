using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NObservable.Blazor")]
namespace NObservable
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ObservableAttribute : Attribute
    {
        
    }
}