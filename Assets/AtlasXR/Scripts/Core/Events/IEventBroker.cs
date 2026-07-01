using System;

namespace AtlasXR.Core.Events
{
    public interface IEventBroker
    {
        void Publish<TEvent>(TEvent eventData) where TEvent : IEvent;

        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    }
}
