using System;
using System.Collections.Generic;

namespace AtlasXR.Core.Events
{
    public sealed class EventBroker : IEventBroker
    {
        private readonly Dictionary<Type, List<Delegate>> handlersByEventType = new Dictionary<Type, List<Delegate>>();

        public void Publish<TEvent>(TEvent eventData) where TEvent : IEvent
        {
            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            var eventType = typeof(TEvent);
            if (!handlersByEventType.TryGetValue(eventType, out var handlers))
            {
                return;
            }

            var snapshot = handlers.ToArray();
            foreach (var handler in snapshot)
            {
                ((Action<TEvent>)handler).Invoke(eventData);
            }
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            var eventType = typeof(TEvent);
            if (!handlersByEventType.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                handlersByEventType[eventType] = handlers;
            }

            handlers.Add(handler);
            return new Subscription(() => Unsubscribe(eventType, handler));
        }

        private void Unsubscribe(Type eventType, Delegate handler)
        {
            if (!handlersByEventType.TryGetValue(eventType, out var handlers))
            {
                return;
            }

            handlers.Remove(handler);
            if (handlers.Count == 0)
            {
                handlersByEventType.Remove(eventType);
            }
        }

        private sealed class Subscription : IDisposable
        {
            private readonly Action dispose;
            private bool isDisposed;

            public Subscription(Action dispose)
            {
                this.dispose = dispose;
            }

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;
                dispose.Invoke();
            }
        }
    }
}
