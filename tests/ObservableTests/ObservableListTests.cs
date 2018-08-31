using System;
using System.Linq;
using System.Threading.Tasks;
using NObservable;
using Xunit;
using static NObservable.Observe;
namespace ObservableTests
{
    public class ObservableListTests : TestBase
    {
        [Fact]
        public void Observable_List_Should_Trigger_On_Particular_Item_Change()
        {
            var l = new ObservableList<int>()
            {
                1, 2, 3
            };
            int triggerCount = 0;
            var t = When(() =>
            {
                triggerCount++;
                return l[1] == 5;
            });
            
            Assert.False(t.IsCompleted);
            Assert.Equal(1, triggerCount);

            l[0] = 5;
            l[2] = 4;
            
            Assert.False(t.IsCompleted);
            Assert.Equal(1, triggerCount);

            l[1] = 4;
            
            Assert.False(t.IsCompleted);
            Assert.Equal(2, triggerCount);

            l[1] = 5;
            Assert.True(t.IsCompleted);
            Assert.Equal(3, triggerCount);
        }
        
        [Fact]
        public void Observable_List_Should_Trigger_On_Item_Count_Change()
        {
            var l = new ObservableList<int>()
            {
                1, 2, 3
            };
            int triggerCount = 0;
            When(() =>
            {
                
                triggerCount++;
                return l[1] == 100;
            });
            
            Assert.Equal(1, triggerCount);

            l.Add(4);
            Assert.Equal(2, triggerCount);

            l.Remove(4);
            Assert.Equal(3, triggerCount);
            
            l.RemoveAt(2);
            Assert.Equal(4, triggerCount);
            
            l.Clear();
            Assert.Equal(5, triggerCount);
        }

        Task WhenTwice(Action cb)
        {
            int cnt = 0;
            return When(() =>
            {
                cnt++;
                if (cnt == 2)
                    return true;
                cb();
                return false;
            });
        }
        
        [InlineData(false)]
        [InlineData(true)]
        [Theory]
        public void Observable_List_Info_Operations_Should_Subscribe_To_Version(bool triggerViaIndexer)
        {
            var lst = new ObservableList<int>()
            {
                1, 2, 3
            };
            var copyTo = WhenTwice(() => lst.CopyTo(new int[3], 0));
            // ReSharper disable ReturnValueOfPureMethodIsNotUsed
            var contains = WhenTwice(() => lst.Contains(5));
            var indexOf = WhenTwice(() => lst.IndexOf(5));
            var enumerator = WhenTwice(() => lst.GetEnumerator());
            // ReSharper restore ReturnValueOfPureMethodIsNotUsed

            var arr = new[] {copyTo, contains, indexOf, enumerator};
            Assert.True(arr.All(t =>!t.IsCompleted));

            if (triggerViaIndexer)
                lst[0] = 5;
            else
                lst.Add(5);
            Assert.True(arr.All(t => t.IsCompleted));
        }
        
    }
}