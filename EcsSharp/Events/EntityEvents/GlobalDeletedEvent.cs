using System;
using EcsSharp.Events.EventArgs;

namespace EcsSharp.Events.EntityEvents
{
    public class GlobalDeletedEvent: DelegateWrapper<EntitiesDeletedEventArgs>
    {
        public static GlobalDeletedEvent operator +(GlobalDeletedEvent d, Action<EntitiesDeletedEventArgs> callback) => (GlobalDeletedEvent)d.AddCallback(callback);

        public static GlobalDeletedEvent operator -(GlobalDeletedEvent d, Action<EntitiesDeletedEventArgs> callback) => (GlobalDeletedEvent)d.RemoveCallback(callback);
    }
}