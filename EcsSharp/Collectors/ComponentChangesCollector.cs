using System;
using System.Collections.Generic;
using System.Linq;
using EcsSharp.Events.EventArgs;

namespace EcsSharp.Collectors
{
    public class ComponentChangesCollector : IDisposable
    {
        private readonly IEcsRepo                                     m_ecsRepo;
        private readonly Dictionary<Type, ComponentUpdatesAggregator> m_typeToAggregatorMap = new();
        private readonly HashSet<Type>                                m_typesSet;
        private readonly List<EntityDeletedEventArgs>                 m_deleted;
        private readonly object                                       m_sync = new object();

        public ComponentChangesCollector(IEcsRepo ecsRepo, Type[] componentTypes)
        {
            m_typesSet = new HashSet<Type>(componentTypes);
            m_ecsRepo = ecsRepo;
            m_ecsRepo.Events.GlobalUpdated += onUpdated;
            m_ecsRepo.Events.GlobalDeleted += onDeleted;
            foreach (Type componentType in componentTypes)
            {
                ComponentUpdatesAggregator ca = new ComponentUpdatesAggregator(componentType);
                m_typeToAggregatorMap[componentType] = ca;
            }

            m_deleted = new List<EntityDeletedEventArgs>();
        }

        public bool HasUpdates { get; private set; }

        public CollectorReport Pop()
        {
            CollectorReport res = null;
            EntityDeletedEventArgs[] deleted;

            ComponentUpdatedEventArgs[] updated;
            lock(m_sync)
            {
                updated = m_typeToAggregatorMap.Values.SelectMany(s => s.PopAllMessages()).ToArray();
                deleted = m_deleted.OrderBy(s => s.ModifiedDate).ToArray();
                m_deleted.Clear();
                HasUpdates = false;
            }

            return createReport(updated, deleted);
        }

        private static CollectorReport createReport(ComponentUpdatedEventArgs[] updated, EntityDeletedEventArgs[] deleted)
        {
            Dictionary<IEntity, List<ComponentUpdatedEventArgs>> dict = new Dictionary<IEntity, List<ComponentUpdatedEventArgs>>();
            foreach (ComponentUpdatedEventArgs c in updated)
            {
                IEntity entity = c.Entity;
                if (!dict.TryGetValue(entity, out List<ComponentUpdatedEventArgs> l))
                {
                    l = new List<ComponentUpdatedEventArgs>();
                    dict[entity] = l;
                }

                l.Add(c);
            }

            EntityUpdatedEventArgs[] entityUpdatedEventArgsArray = dict.Select(pair => new EntityUpdatedEventArgs(pair.Key, pair.Value.ToArray())).ToArray();
            return new CollectorReport
            {
                Updated = entityUpdatedEventArgsArray,
                Deleted = deleted
            };
        }

        private void onDeleted(EntitiesDeletedEventArgs obj)
        {
            foreach (EntityDeletedEventArgs ea in obj)
            {
                foreach (ComponentDeletedEventArgs dc in ea.DeletedComponents)
                {
                    if (m_typesSet.Contains(dc.ComponentType))
                    {
                        lock(m_sync)
                        {
                            m_deleted.Add(ea);
                            HasUpdates = true;
                        }

                        break;
                    }
                }
            }
        }

        private void onUpdated(EntitiesUpdatedEventArgs entitiesUpdatedEventArgs)
        {
            lock(m_sync)
            {
                foreach (EntityUpdatedEventArgs args in entitiesUpdatedEventArgs)
                {
                    foreach (ComponentUpdatedEventArgs componentUpdatedEventArgs in args.UpdatedComponents)
                    {
                        if (m_typeToAggregatorMap.TryGetValue(componentUpdatedEventArgs.ComponentType, out ComponentUpdatesAggregator aggregator))
                        {
                            aggregator.Add(componentUpdatedEventArgs);
                            HasUpdates = true;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            CollectorReport _ = Pop();
            m_ecsRepo.Events.GlobalUpdated -= onUpdated;
            m_ecsRepo.Events.GlobalDeleted -= onDeleted;
        }
    }

    internal class ComponentUpdatesAggregator
    {
        public Type ComponentType { get; }

        private readonly Dictionary<string, ComponentUpdatedEventArgs> m_internalMap = new();
        private readonly object                                        m_sync        = new();

        public ComponentUpdatesAggregator(Type componentType) => ComponentType = componentType;

        public void Add(ComponentUpdatedEventArgs args)
        {
            lock(m_sync)
            {
                m_internalMap[args.Entity.Id] = args;
            }
        }

        public ComponentUpdatedEventArgs[] PopAllMessages()
        {
            lock(m_sync)
            {
                ComponentUpdatedEventArgs[] array = m_internalMap.Values.ToArray();
                m_internalMap.Clear();
                return array;
            }
        }
    }

    public class CollectorReport
    {
        public EntityUpdatedEventArgs[] Updated { get; init; }
        public EntityDeletedEventArgs[] Deleted { get; init; }
    }
}