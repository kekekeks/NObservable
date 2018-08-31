using System;
using System.Threading;
using NObservable.Scheduling;

namespace NObservable
{
    public static class NObservableEngine
    {
        [ThreadStatic] private static Context _context;
        internal static Context Context => _context ?? (_context = new Context());
        
        public static IScheduler NObservableScheduler
        {
            get => Context.Scheduler;
            set => Context.Scheduler = value;
        }

        public static IDisposable Autorun(Action action) => Autorun(action, null);
        
        public static IDisposable Autorun(Action action, AutorunOptions options)
        {
            var ids = Context.TrackUsedValues(action);
            var subscription = new Subscription(Context);
            subscription.Action = () =>
            {
                var newIds = Context.TrackUsedValues(action);
                // Subscription was disposed
                if (subscription.Subscriptions == null)
                    return;
                Context.ReplaceSubscriptions(subscription, newIds);
            };
            
            if (options != null)
            {
                subscription.Scheduler = options.Scheduler;
                subscription.Delay = options.Delay;
            }

            Context.Subscribe(subscription, ids);
            return subscription;
        }

        public static void RunInAction(Action action)
        {
            Context.RunInAction(action);
        }
    }
}