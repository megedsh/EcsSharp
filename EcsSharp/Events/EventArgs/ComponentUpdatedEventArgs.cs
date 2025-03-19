using System;

namespace EcsSharp.Events.EventArgs
{
    public class ComponentUpdatedEventArgs : ComponentEventArgs
    {
        public Component OldComponent { get; }

        public ComponentUpdatedEventArgs(IEntity entity,
                                         Type componentType,
                                         Component component,
                                         Component oldComponent) : base(entity, componentType, component)
        {
            OldComponent = oldComponent;
        }
    }
}