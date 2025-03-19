using System;
using EcsSharp.Events.EventArgs;

namespace EcsSharp.Events.EntityEvents
{
    public class GlobalUpdatedEvent : DelegateWrapper<EntitiesUpdatedEventArgs>
    {
        public static GlobalUpdatedEvent operator +(GlobalUpdatedEvent d, Action<EntitiesUpdatedEventArgs> callback) => (GlobalUpdatedEvent)d.AddCallback(callback);

        public static GlobalUpdatedEvent operator -(GlobalUpdatedEvent d, Action<EntitiesUpdatedEventArgs> callback) => (GlobalUpdatedEvent)d.RemoveCallback(callback);
    }
}