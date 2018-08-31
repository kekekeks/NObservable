using NObservable;
using NObservable.Scheduling;

namespace ObservableTests
{
    public class TestBase
    {
        public TestBase()
        {
            if(NObservableEngine.NObservableScheduler == null)
                NObservableEngine.NObservableScheduler = new ImmediateScheduler();
        }
    }
}