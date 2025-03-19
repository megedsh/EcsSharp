using System.Collections.Generic;
using EcsSharp.Events.EventArgs;

namespace EcsSharp.Storage
{
    public class BatchEventsCollector
    {
        private readonly List<EntityCreatedEventArgs>    m_entityCreatedList    = new List<EntityCreatedEventArgs>();
        private readonly List<ComponentDeletedEventArgs> m_componentDeletedList = new List<ComponentDeletedEventArgs>();
        private readonly List<ComponentCreatedEventArgs> m_componentCreatedList = new List<ComponentCreatedEventArgs>();
        private readonly List<ComponentUpdatedEventArgs> m_componentUpdatedList = new List<ComponentUpdatedEventArgs>();
        public EntityCreatedEventArgs[] EntityCreatedEventsArgs => m_entityCreatedList.ToArray();
        public ComponentCreatedEventArgs[] ComponentCreatedEventArgs => m_componentCreatedList.ToArray();
        public ComponentUpdatedEventArgs[] ComponentUpdatedEventArgs => m_componentUpdatedList.ToArray();
        public ComponentDeletedEventArgs[] ComponentDeletedEventArgs  => m_componentDeletedList.ToArray();

        public void Add(EntityCreatedEventArgs[] entityCreatedArgs)
        {
            m_entityCreatedList.AddRange(entityCreatedArgs);
        }

        public void Add(ComponentDeletedEventArgs[] componentDeletedArgs)
        {
            m_componentDeletedList.AddRange(componentDeletedArgs);
        }

        public void Add(ComponentCreatedEventArgs[] componentCreatedArgs)
        {
            m_componentCreatedList.AddRange(componentCreatedArgs);
        }

        public void Add(ComponentUpdatedEventArgs[] componentUpdatedArgs)
        {
            m_componentUpdatedList.AddRange(componentUpdatedArgs);
        }
    }
}