using System;
using System.Threading.Tasks;
using NObservable;
using Xunit;

namespace ObservableTests
{
    public class WhenTests : TestBase
    {
        [Observable]
        class Observable
        {
            public int Value { get; set; }
        }

        [Fact]
        public void When_Effect_Should_Trigger_Once_When_Condition_Is_Reached()
        {
            var ob = new Observable();
            int triggerNumber = 0;
            Observe.When(() => ob.Value == 10, () => { triggerNumber++; });
            ob.Value = 5;
            ob.Value = 6;
            Assert.Equal(0, triggerNumber);
            ob.Value = 10;
            Assert.Equal(1, triggerNumber);
            ob.Value = 5;
            ob.Value = 10;
            Assert.Equal(1, triggerNumber);
        }
        
        [Fact]
        public void When_Should_Be_Disposable()
        {
            var ob = new Observable();
            int triggerNumber = 0;
            var d = Observe.When(() => ob.Value == 10, () => { triggerNumber++; });
            ob.Value = 5;
            ob.Value = 6;
            Assert.Equal(0, triggerNumber);
            d.Dispose();
            ob.Value = 10;
            Assert.Equal(0, triggerNumber);
            ob.Value = 5;
            ob.Value = 10;
            Assert.Equal(0, triggerNumber);
        }

        [Fact]
        public void When_Task_Should_Trigger_When_Condition_Is_Reached()
        {
            var ob = new Observable();
            var triggered = false;
            Observe.When(() => ob.Value == 10)
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                        triggered = true;
                }, TaskContinuationOptions.ExecuteSynchronously);
            ob.Value = 5;
            Assert.False(triggered);
            ob.Value = 10;
            Assert.True(triggered);
        }
        
        [Fact]
        public void When_Task_Should_Propagate_Condition_Exceptions()
        {
            var ob = new Observable();
            var triggered = false;
            var exception = false;
            Observe.When(() =>
                {
                    if(ob.Value == 10)
                        throw new Exception("TEST");
                    return false;
                })
                .ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                        triggered = true;
                    if (t.IsFaulted && t.Exception.InnerException.Message == "TEST")
                        exception = true;
                }, TaskContinuationOptions.ExecuteSynchronously);
            ob.Value = 5;
            Assert.False(triggered);
            Assert.False(exception);
            ob.Value = 10;
            Assert.False(triggered);
            Assert.True(exception);
            
        }
        
    }
}