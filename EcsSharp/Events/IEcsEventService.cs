using EcsSharp.Events.ComponentEvents;
using EcsSharp.Events.EntityEvents;
using EcsSharp.Events.EventArgs;
using EcsSharp.Events.TagEvents;

namespace EcsSharp.Events
{
    public interface IEcsEventService
    {
        ComponentCreatedEvents ComponentCreated { get; }
        ComponentUpdatedEvents ComponentUpdated { get; }
        ComponentDeletedEvents ComponentDeleted { get; }

        GlobalCreatedEvent GlobalCreated { get; set; }
        GlobalUpdatedEvent GlobalUpdated { get; set; }
        GlobalDeletedEvent GlobalDeleted { get; set; }

        TagCreatedEvents TaggedEntitiesCreated { get; set; }
        TagUpdatedEvents TaggedEntitiesUpdated { get; set; }
        TagDeletedEvents TaggedEntitiesDeleted { get; set; }

        SpecificCreatedEvent SpecificCreated { get; set; }
        SpecificUpdatedEvent SpecificUpdated { get; set; }
        SpecificDeletedEvent SpecificDeleted { get; set; }

        void InvokeComponentUpdatedDelegates(ComponentUpdatedEventArgs[] args);
        void InvokeComponentCreatedDelegates(ComponentCreatedEventArgs[] args);
        void InvokeComponentDeletedDelegates(ComponentDeletedEventArgs[] args);
        void InvokeEntityCreatedDelegates(EntityCreatedEventArgs[] args);
    }
}