namespace EcsSharp.Events.EventArgs
{
    public class EntityUpdatedEventArgs : EntityEventArgs<ComponentUpdatedEventArgs>
    {
        public EntityUpdatedEventArgs(IEntity entity, ComponentUpdatedEventArgs[] updatedComponents):base(entity,updatedComponents)
        {
        }

        public ComponentUpdatedEventArgs[] UpdatedComponents => ComponentEventArgs;
    }
}