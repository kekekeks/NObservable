using System;
using System.ComponentModel;

namespace NObservable.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct PropertyTracker
    {
        private Context _context;
        internal int ObjectId;

        public static PropertyTracker Create() => new PropertyTracker()
        {
            _context = Observe.Context,
            ObjectId = Observe.Context.NextObjectId++
        };

        public static void Init(ref PropertyTracker field)
        {
            if (field._context == null)
                field = Create();
        }

        public void TrackGet(int token)
        {
            if (Observe.Context == _context)
            {
                _context.TrackGet(ObjectId, token);
            }
            else if(Observe.Context.IsTracking)
                throw new InvalidOperationException("Call from non-owner thread");
        }

        public void TrackSet<T>(int token, T oldValue, T newValue)
        {
            if (!Equals(oldValue, newValue))
                _context.TrackSet(ObjectId, token);
        }

        public void EnterTrackSet()
        {
            if (Observe.Context != _context)
                throw new InvalidOperationException("Call from non-owner thread");
            _context.PauseGetTracking();
        }

        public void LeaveTrackSet()
        {
            _context.ResumeGetTracking();
        }
    }
}