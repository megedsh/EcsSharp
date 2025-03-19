using System;

namespace EcsSharp.Events.EventArgs
{
    [Serializable]
    public class ComponentDeletedEventArgs: ComponentEventArgs
    {
        public ComponentDeletedEventArgs(IEntity entity, Type componentType, Component component) : base(entity, componentType,  component)
        {
        }
    }
}