using NObservable;
using NObservable.Configuration;
using NObservable.Scheduling;

namespace ObservableTests
{
    public class TestBase
    {
        public TestBase()
        {
            // Reset scheduler even if it was changed by a previous test run
            NObservableConfiguration.CurrentThread.Scheduler = new ImmediateScheduler();
        }
    }
}