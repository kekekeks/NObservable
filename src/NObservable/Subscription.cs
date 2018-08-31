using System;
using System.Collections.Generic;
using NObservable.Scheduling;

namespace NObservable
{
   
    sealed class Subscription : IDisposable
    {
        public List<TrackedValueId> Subscriptions { get; set; }
        public Action Action { get; set; }
        public IScheduler Scheduler { get; set; }
        public TimeSpan? Delay { get; set; }
        private readonly Context _context;

        public Subscription(Context context)
        {
            _context = context;
        }
        
        public void Dispose()
        {
            if(NObservableEngine.Context != _context)
                throw new InvalidOperationException("Call from non-owner thread");
            if (Subscriptions != null)
            {
                _context.Unsubscribe(this);
                Subscriptions = null;
            }
        }
    }

    struct TrackedValueId
    {
        public int ObjectId { get; }
        public int PropertyId { get;  }
        
        public TrackedValueId(int objectId, int propertyId)
        {
            ObjectId = objectId;
            PropertyId = propertyId;
        }

        public bool Equals(TrackedValueId other)
        {
            return ObjectId == other.ObjectId && PropertyId == other.PropertyId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is TrackedValueId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (ObjectId * 397) ^ PropertyId;
            }
        }
    }
}