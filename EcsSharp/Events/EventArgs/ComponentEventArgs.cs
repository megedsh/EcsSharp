using System;

namespace EcsSharp.Events.EventArgs
{
    [Serializable]
    public abstract class ComponentEventArgs
    {
        public IEntity Entity { get; }
        public Type ComponentType { get; }
        public Component Component { get; }
        public DateTime ModifiedDate { get; }

        protected ComponentEventArgs(IEntity entity, Type componentType, Component component)
        {
            Entity = entity;
            ComponentType = componentType;
            Component = component;
            ModifiedDate = DateTime.UtcNow;
        }
    }
}