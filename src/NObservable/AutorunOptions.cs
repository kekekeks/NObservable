using System;
using NObservable.Scheduling;

namespace NObservable
{
    public class AutorunOptions
    {
        public IScheduler Scheduler { get; set; }
        public TimeSpan? Delay { get; set; }
    }
}