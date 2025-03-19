namespace EcsSharp.Events.EventArgs
{
    public class EntityCreatedEventArgs : EntityEventArgs<ComponentCreatedEventArgs>
    {
        public EntityCreatedEventArgs(IEntity entity, ComponentCreatedEventArgs[] componentArgs) : base(entity, componentArgs)
        {
        }

        public ComponentCreatedEventArgs[] CreatedComponents => ComponentEventArgs;

    }
}