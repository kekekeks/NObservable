using System;

namespace NObservable
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ObservableAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Computed : Attribute
    {
        
    }
}