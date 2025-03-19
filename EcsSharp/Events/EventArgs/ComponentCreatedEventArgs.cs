using System;

namespace EcsSharp.Events.EventArgs
{
    [Serializable]
    public class ComponentCreatedEventArgs : ComponentEventArgs
    {
        public ComponentCreatedEventArgs(IEntity entity, Type componentType, Component component) : base(entity, componentType, component)
        {
        }
    }
}