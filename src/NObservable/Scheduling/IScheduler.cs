using System;

namespace NObservable.Scheduling
{
    public interface IScheduler
    {
        void Execute(Action action, TimeSpan? delayed = null);
    }
}