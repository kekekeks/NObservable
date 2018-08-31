using System;

namespace NObservable.Internals
{
    public struct PropertyTracker
    {
        private Context _context;
        private int _objectId;

        public static PropertyTracker Create() => new PropertyTracker()
        {
            _context = NObservableEngine.Context,
            _objectId = NObservableEngine.Context.NextObjectId++
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
                _context.TrackGet(_objectId, token);
            }
            else if(NObservableEngine.Context.IsTracking)
                throw new InvalidOperationException("Call from non-owner thread");
        }

        public void TrackSet<T>(int token, T oldValue, T newValue)
        {
            if (!Equals(oldValue, newValue))
                _context.TrackSet(_objectId, token);
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