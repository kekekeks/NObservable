using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Blazor.Components;

namespace NObservable.Blazor.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class NObservableBlazorComponentHelper
    {
        private readonly BlazorComponent _component;
        private Subscription _subscription;
        private List<TrackedValueId> _trackedValues;
        private int _insideRender;

        private static Action<BlazorComponent> _stateChanged =
            (Action<BlazorComponent>)
            Delegate.CreateDelegate(typeof(Action<BlazorComponent>),
                typeof(BlazorComponent).GetMethod("StateHasChanged",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));



        public NObservableBlazorComponentHelper(BlazorComponent component)
        {
            _component = component;
        }

        public void OnRenderEnter()
        {
            if (_subscription != null)
            {
                _subscription.Dispose();
                _subscription = null;
            }
            if (_insideRender == 0)
            {
                _trackedValues = Observe.Context.StartTrackingSession();
            }
            _insideRender++;
            
        }

        public void OnRenderLeave()
        {
            _insideRender--;
            if (_insideRender == 0)
            {
                Observe.Context.EndTrackingSession();
                var subscription = new Subscription(Observe.Context);
                subscription.Action = () =>
                {
                    subscription.Dispose();
                    if (_subscription == subscription)
                    {
                        _subscription = null;
                        _stateChanged(_component);
                    }
                };
                Observe.Context.Subscribe(subscription, _trackedValues);
                _subscription = subscription;
            }
        }
    }
}