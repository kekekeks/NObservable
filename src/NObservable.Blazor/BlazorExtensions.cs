using System;
using System.Threading;
using NObservable.Scheduling;

namespace Microsoft.AspNetCore.Blazor.Builder
{
    public static class NObservableBlazorExtensions
    {
        public static void UseNObservable(this IBlazorApplicationBuilder app)
        {
            NObservable.Configuration.NObservableConfiguration.CurrentThread.Scheduler =
                new SynchronizationContextScheduler(SynchronizationContext.Current);
        }
    }

}