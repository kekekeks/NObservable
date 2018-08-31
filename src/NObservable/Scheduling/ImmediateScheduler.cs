using System;

namespace NObservable.Scheduling
{
    public class ImmediateScheduler : IScheduler
    {
        public void Execute(Action action, TimeSpan? delayed = null)
        {
            action();
        }
    }
}