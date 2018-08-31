using System;
using System.Collections.Generic;
using System.Linq;
using NObservable;
using NObservable.Internals;
using Xunit;

namespace ObservableTests
{
    public class BasicEngineTest : TestBase
    {
        void Wtf()
        {
            var tracker = PropertyTracker.Create();
            tracker.TrackSet(1, 1, 1);
        }
        
        class PropertyHelper<T>
        {
            private readonly int _token;
            public PropertyHelper(int token)
            {
                _token = token;
            }
            private T _backingField;
            private PropertyTracker _tracker = PropertyTracker.Create();
            public T Value
            {
                get
                {
                    _tracker.TrackGet(_token);
                    return _backingField;
                }
                set
                {
                    var old = _backingField;
                    _backingField = value;
                    _tracker.TrackSet(_token, old, value);
                }
            }
        }
        
        
        [Fact]
        public void Autorun_Should_Be_Triggered_On_Property_Set()
        {
            var p = new PropertyHelper<int>(1);
            var p2 = new PropertyHelper<int>(2);
            p.Value = 1;
            var sequence = new List<int>();
            
            NObservableEngine.Autorun(() =>
            {
                sequence.Add(p.Value);
                if (p.Value == 10)
                    sequence.Add(p2.Value);
            });
            p.Value = 2;
            p2.Value = 2;
            p.Value = 3;
            p2.Value = 3;
            p.Value = 10;
            p2.Value = 10;

            Assert.True(sequence.SequenceEqual(new[] {1, 2, 3, 10, 3, 10, 10}));
        }

        [Fact]
        public void Autorun_Should_Be_Unsubcribeable()
        {
            var p = new PropertyHelper<int>(1) {Value = 1};
            var seq = new List<int>();
            var d = NObservableEngine.Autorun(() => seq.Add(p.Value));
            p.Value = 2;
            d.Dispose();
            p.Value = 3;
            Assert.True(seq.SequenceEqual(new []{1, 2}));
        }
        
        [Fact]
        public void Autorun_Should_Be_Able_To_Unsubcribe()
        {
            var p = new PropertyHelper<int>(1) {Value = 1};
            var seq = new List<int>();
            IDisposable d = null;
            d = NObservableEngine.Autorun(() =>
            {
                seq.Add(p.Value);
                if(p.Value == 2)
                    d.Dispose();
            });
            p.Value = 2;
            p.Value = 3;
            Assert.True(seq.SequenceEqual(new []{1, 2}));
        }

        [Fact]
        public void Multiple_Property_Changes_Should_Be_Grouped_Inside_Action()
        {
            var p1 = new PropertyHelper<int>(1) {Value = 1};
            var p2 = new PropertyHelper<int>(2) {Value = 1};
            var seq = new List<int>();
            NObservableEngine.Autorun(() =>
            {
                seq.Add(p1.Value);
                seq.Add(p2.Value);
            });
            NObservableEngine.RunInAction(() =>
            {
                p1.Value = 2;
                p2.Value = 2;
            });
            NObservableEngine.RunInAction(() =>
            {
                p1.Value = 3;
                p1.Value = 4;
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