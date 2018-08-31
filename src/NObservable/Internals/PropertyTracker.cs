using System;

namespace NObservable.Internals
{
    public struct PropertyTracker
    {
        private Context _context;
        internal int ObjectId;

        public static PropertyTracker Create() => new PropertyTracker()
        {
            _context = NObservableEngine.Context,
            ObjectId = NObservableEngine.Context.NextObjectId++
        };

        public static void Init(ref PropertyTracker field)
        {
            if (field._context == null)
                field = Create();
        }

        public void TrackGet(int token)
        {
            if (NObservableEngine.Context == _context)
            {
                _context.TrackGet(ObjectId, token);
            }
            else if(NObservableEngine.Context.IsTracking)
                throw new InvalidOperationException("Call from non-owner thread");
        }

        public void TrackSet<T>(int token, T oldValue, T newValue)
        {
            if (!Equals(oldValue, newValue))
                _context.TrackSet(ObjectId, token);
        }

        public void EnterTrackSet()
        {
            if (NObservableEngine.Context != _context)
                throw new InvalidOperationException("Call from non-owner thread");
            _context.PauseGetTracking();
        }

        public void LeaveTrackSet()
        {
            _context.ResumeGetTracking();
        }
    }
}