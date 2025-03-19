namespace EcsSharp.Events.EventArgs
{
    public class EntityDeletedEventArgs : EntityEventArgs<ComponentDeletedEventArgs>
    {
        public EntityDeletedEventArgs(IEntity entity, ComponentDeletedEventArgs[] deletedComponents):base(entity,deletedComponents)
        {
        }

        public ComponentDeletedEventArgs[] DeletedComponents => ComponentEventArgs;
    }
}