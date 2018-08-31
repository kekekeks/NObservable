using System.Collections;
using System.Collections.Generic;
using NObservable.Internals;
using static NObservable.NObservableEngine;
namespace NObservable
{
    public sealed class ObservableList<T> : IList<T>
    {
        private const int CountFieldId = -1;
        private const int VersionFieldId = -2;

        private PropertyTracker _tracker = PropertyTracker.Create();
        private readonly List<T> _inner = new List<T>();

        private int _count;
        private int _version;
        public int Count
        {
            get
            {
                _tracker.TrackGet(CountFieldId);
                return _inner.Count;
            }
        }

        void UpdateCount()
        {
            var old = _count;
            _count = _inner.Count;
            _tracker.TrackSet(CountFieldId, old, _count);
        }

        void TrackVersion() => _tracker.TrackGet(VersionFieldId);
        void UpdateVersion() => _tracker.TrackSet(VersionFieldId, _version, ++_version);

        void InvalidateList()
        {
            RunInAction(() =>
            {
                UpdateCount();
                UpdateVersion();
            });
        }
        
        public T this[int index]
        {
            get
            {
                // Invalidate this read when either item count or this particular item change
                _tracker.TrackGet(CountFieldId);
                _tracker.TrackGet(index);
                return _inner[index];
            }
            set
            {
                T old;
                _tracker.EnterTrackSet();
                try
                {
                    old = _inner[index];
                }
                finally
                {
                    _tracker.LeaveTrackSet();
                }
                _inner[index] = value;
                RunInAction(() =>
                {
                    _tracker.TrackSet(index, old, value);
                    UpdateVersion();
                });
            }
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            TrackVersion();
            return _inner.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(T item)
        {
            TrackVersion();
            return _inner.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            TrackVersion();
            _inner.CopyTo(array, arrayIndex);
        }
        
        public bool IsReadOnly => false;
        public int IndexOf(T item)
        {
            TrackVersion();
            return _inner.IndexOf(item);
        }
        
        
        public void Add(T item)
        {
            _inner.Add(item);
            InvalidateList();
        }

        public void Clear()
        {
            _inner.Clear();
            InvalidateList();
        }

        public void Insert(int index, T item)
        {
            _inner.Insert(index, item);
            InvalidateList();
        }

        public void RemoveAt(int index)
        {
            _inner.RemoveAt(index);
            InvalidateList();
        }
        
        public bool Remove(T item)
        {
            var idx = _inner.IndexOf(item);
            if (idx == -1)
                return false;
            RemoveAt(idx);
            return true;
        }
    }
}