using System;
using System.Threading;
using System.Threading.Tasks;

namespace NObservable.Scheduling
{
    public class SynchronizationContextScheduler : IScheduler
    {
        private readonly SynchronizationContext _synchronizationContext;

        public SynchronizationContextScheduler(SynchronizationContext synchronizationContext)
        {
            _synchronizationContext = synchronizationContext;
        }
        
        public void Execute(Action action, TimeSpan? delayed = null)
        {
            if (delayed == null)
            {
                if (SynchronizationContext.Current == _synchronizationContext)
                    action();
                else
                    _synchronizationContext.Post(_ => action(), null);
            }
            else
                Task.Delay(delayed.Value).ContinueWith(_ => _synchronizationContext.Post(__ => action(), null));
        }
    }
}