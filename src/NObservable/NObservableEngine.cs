using System;
using System.Threading;
using System.Threading.Tasks;
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

        public static Task When(Func<bool> condition) => When(condition, (AutorunOptions) null);
        public static Task When(Func<bool> condition, AutorunOptions options)
        {
            var tcs = new TaskCompletionSource<int>();
            IDisposable autorun = null;
            autorun = Autorun(() =>
            {
                try
                {
                    if (condition())
                    {
                        autorun?.Dispose();
                        tcs.TrySetResult(0);
                    }
                }
                catch (Exception e)
                {
                    autorun?.Dispose();
                    tcs.TrySetException(e);
                }

            }, options);
            return tcs.Task;
        }

        public static IDisposable When(Func<bool> condition, Action effect) =>
            When(condition, effect, null);
        
        public static IDisposable When(Func<bool> condition, Action effect, AutorunOptions options)
        {
            IDisposable autorun = null;
            autorun = Autorun(() =>
            {
                var runEffect = false;
                var dispose = true;
                try
                {
                    if (condition())
                    {
                        runEffect = true;
                    }
                    else
                    {
                        // Avoid having a catch block in order to not disrupt debugging 
                        // This indicates that exception has NOT happened and condition()==FALSE
                        dispose = false;
                    }
                }
                finally
                {
                    if (dispose)
                        autorun?.Dispose();
                }

                if (runEffect)
                    effect();
            }, options);
            return autorun;
        }
        
        
    }
}