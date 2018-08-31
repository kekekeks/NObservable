using System;

namespace NObservable
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class ObservableAttribute : Attribute
    {
    }
}