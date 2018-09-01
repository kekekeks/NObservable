using System;
using NObservable.Scheduling;

namespace Microsoft.AspNetCore.Blazor.Builder
{
    public static class NObservableBlazorExtensions
    {
        public static void UseNObservable(this IBlazorApplicationBuilder app)
        {
            NObservable.Configuration.NObservableConfiguration.CurrentThread.Scheduler = new BlazorScheduler(app);
        }
    }

    public class BlazorScheduler : IScheduler
    {
        public BlazorScheduler(IBlazorApplicationBuilder app)
        {
            
        }

        public void Execute(Action action, TimeSpan? delayed = null)
        {
            action();
        }
    }
}