using System;

namespace EcsSharp.Events.EventArgs
{
    [Serializable]
    public class ComponentDeletedEventArgs: ComponentEventArgs
    {
        public bool EntityDeleted { get; }

        public ComponentDeletedEventArgs(IEntity entity,
                                         Type componentType, 
                                         Component component, bool entityDeleted) : base(entity, componentType,  component)
        {
            EntityDeleted = entityDeleted;
        }
    }
}