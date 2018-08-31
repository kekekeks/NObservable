using System;
using NObservable.Scheduling;

namespace NObservable.Configuration
{
    public class NObservableThreadConfiguration
    {
        private readonly Context _context;

        internal NObservableThreadConfiguration(Context context)
        {
            _context = context;
        }
        public IScheduler Scheduler
        {
            get => _context.Scheduler;
            set => _context.Scheduler = value;
        }
    }

    public class NObservableConfiguration
    {
        [ThreadStatic] private static NObservableThreadConfiguration _currentThread;

        public static NObservableThreadConfiguration CurrentThread
        {
            get
            {
                if (_currentThread == null)
                    _currentThread = new NObservableThreadConfiguration(Observe.Context);
                return _currentThread;
            }
        }
    }
}