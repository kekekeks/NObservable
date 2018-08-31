using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NObservable.Scheduling;

namespace NObservable
{
    internal class Context
    {
        class ActionContext
        {
            public int Depth { get; set; }
            public HashSet<Subscription> TriggeredSubscribers { get; set; }

            public void Add(Subscription sub)
            {
                if (TriggeredSubscribers == null)
                    TriggeredSubscribers = new HashSet<Subscription>();
                TriggeredSubscribers.Add(sub);
            }
        }
        
        
        public IScheduler Scheduler { get; set; }
        private ActionContext _currentAction;
        public int NextObjectId { get; set; }
        private readonly Stack<List<TrackedValueId>> _trackers = new Stack<List<TrackedValueId>>();
        public bool IsTracking => _trackers.Count != 0 && _pauseTrackingCounter == 0;
        private int _pauseTrackingCounter;

        private readonly Dictionary<TrackedValueId, HashSet<Subscription>> _subscribers =
            new Dictionary<TrackedValueId, HashSet<Subscription>>();

        IScheduler GetScheduler()
        {
            if (Scheduler == null)
            {
                if (SynchronizationContext.Current != null)
                    Scheduler = new SynchronizationContextScheduler(SynchronizationContext.Current);
                else
                    throw new InvalidOperationException(
                        "CurrentScheduler id null and SynchronizationContext.Current is also null");
            }

            return Scheduler;
        }

        public void RunInAction(Action action)
        {
            if (_currentAction== null)
                _currentAction= new ActionContext();
            else
                _currentAction.Depth++;
            try
            {
                action();
            }
            finally
            {
                var ctx = _currentAction;
                if (_currentAction.Depth == 0)
                    _currentAction= null;
                else
                    _currentAction.Depth--;
                if(ctx.TriggeredSubscribers != null)
                    foreach (var item in ctx.TriggeredSubscribers)
                        Schedule(item);
            }
        }

        public void TrackGet(int objectId, int propertyId)
        {
            foreach (var tracker in _trackers)
                tracker.Add(new TrackedValueId(objectId, propertyId));
        }

        public void TrackSet(int objectId, int propertyId)
        {
            var action = _currentAction;
            if (_subscribers.TryGetValue(new TrackedValueId(objectId, propertyId), out var lst))
            {
                List<Subscription> immediate = null;
                foreach (var item in lst)
                {
                    if (action != null)
                        action.Add(item);
                    else
                        (immediate ?? (immediate = new List<Subscription>())).Add(item);
                }

                if(immediate!=null)
                    foreach (var item in immediate)
                        Schedule(item);
            }
        }

        public void Schedule(Subscription subscription)
        {
            (subscription.Scheduler ?? Scheduler).Execute(subscription.Action, subscription.Delay);
        }
        
        public List<TrackedValueId> TrackUsedValues(Action action)
        {
            var values = new List<TrackedValueId>();
            _trackers.Push(values);
            try
            {
                action();
            }
            finally
            {
                _trackers.Pop();
            }
            return values;
        }

        public void Subscribe(Subscription subscription, List<TrackedValueId> valueIds)
        {
            if (subscription.Subscriptions?.Count > 0)
                throw new InvalidOperationException("Subscription already have a value list");
            foreach (var v in valueIds)
            {
                if (!_subscribers.TryGetValue(v, out var list))
                    _subscribers[v] = list = new HashSet<Subscription>();
                list.Add(subscription);
            }
            subscription.Subscriptions = valueIds;
        }

        public void Unsubscribe(Subscription subscription)
        {
            foreach (var v in subscription.Subscriptions)
                if (_subscribers.TryGetValue(v, out var list))
                    list.Remove(subscription);
            subscription.Subscriptions = null;
        }

        public void ReplaceSubscriptions(Subscription subscription, List<TrackedValueId> newValueIds)
        {
            Unsubscribe(subscription);
            Subscribe(subscription, newValueIds);
        }

        public void PauseGetTracking()
        {
            _pauseTrackingCounter++;
        }

        public void ResumeGetTracking()
        {
            _pauseTrackingCounter--;
            if (_pauseTrackingCounter < 0)
                throw new InvalidOperationException("Unbalanced calls");
        }
    }
}