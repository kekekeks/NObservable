using System.Collections.Generic;
using System.Linq;
using NObservable;
using Xunit;

namespace ObservableTests
{
    public class InstrumentationTests : TestBase
    {
        [Observable]
        public class MyObservable
        {
            public int Prop1 { get; set; }
            public int Prop2 { get; set; }
        }
        
        
        [Fact]
        public void Autorun_Should_Be_Triggered_On_Property_Set()
        {
            var o = new MyObservable {Prop1 = 1};
            var sequence = new List<int>();
            
            NObservableEngine.Autorun(() =>
            {
                sequence.Add(o.Prop1);
                if (o.Prop1 == 10)
                    sequence.Add(o.Prop2);
            });
            o.Prop1 = 2;
            o.Prop2 = 2;
            o.Prop1 = 3;
            o.Prop2 = 3;
            o.Prop1 = 10;
            o.Prop2 = 10;

            Assert.True(sequence.SequenceEqual(new[]
            {
                1,
                2,
                3,
                10, 3,
                10, 10
            }));
        }

        
        [Fact]
        public void Multiple_Property_Changes_Should_Be_Grouped_Inside_Action()
        {
            var o = new MyObservable
            {
                Prop1 = 1,
                Prop2 = 1
            };
            var seq = new List<int>();
            NObservableEngine.Autorun(() =>
            {
                seq.Add(o.Prop1);
                seq.Add(o.Prop2);
            });
            NObservableEngine.RunInAction(() =>
            {
                o.Prop1 = 2;
                o.Prop2 = 2;
            });
            NObservableEngine.RunInAction(() =>
            {
                o.Prop1 = 3;
                o.Prop1 = 4;
            });
            Assert.True(seq.SequenceEqual(new []
            {
                1, 1,
                2, 2,
                4, 2
            }));
        }
    }
}