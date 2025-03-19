using System;
using EcsSharp.Events.EventArgs;

namespace EcsSharp.Events.EntityEvents
{
    public class GlobalCreatedEvent : DelegateWrapper<EntitiesCreatedEventArgs>
    {
        public static GlobalCreatedEvent operator +(GlobalCreatedEvent d, Action<EntitiesCreatedEventArgs> callback) => (GlobalCreatedEvent)d.AddCallback(callback);

        public static GlobalCreatedEvent operator -(GlobalCreatedEvent d, Action<EntitiesCreatedEventArgs> callback) => (GlobalCreatedEvent)d.RemoveCallback(callback);
    }
}