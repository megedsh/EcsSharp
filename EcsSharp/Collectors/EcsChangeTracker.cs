using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using EcsSharp.Events;
using EcsSharp.Events.EventArgs;

namespace EcsSharp.Collectors
{
    public interface IEcsChangeTracker
    {
        IEcsChangeSubscription Subscribe(Type[] componentTypes);
    }

    public interface IEcsChangeSubscription : IDisposable
    {
        CollectorReport Pop();
    }

    internal interface IEcsChangeRecorder
    {
        void RecordUpdated(ComponentUpdatedEventArgs[] updates);
        void RecordDeleted(ComponentDeletedEventArgs[] deleted);
    }

    internal sealed class EcsChangeTracker : IEcsChangeTracker, IEcsChangeRecorder
    {
        private readonly object m_sync = new object();

        private Dictionary<Type, EcsChangeSubscription[]> m_subscriptionsByType = new();
        private EcsChangeSubscription[] m_subscriptions = Array.Empty<EcsChangeSubscription>();

        public IEcsChangeSubscription Subscribe(Type[] componentTypes)
        {
            EcsChangeSubscription subscription = new EcsChangeSubscription(componentTypes, remove);
            lock (m_sync)
            {
                EcsChangeSubscription[] subscriptions = new EcsChangeSubscription[m_subscriptions.Length + 1];
                Array.Copy(m_subscriptions, subscriptions, m_subscriptions.Length);
                subscriptions[subscriptions.Length - 1] = subscription;
                publish(subscriptions);
            }

            return subscription;
        }

        public void RecordUpdated(ComponentUpdatedEventArgs[] updates)
        {
            Dictionary<Type, EcsChangeSubscription[]> index = Volatile.Read(ref m_subscriptionsByType);
            foreach (ComponentUpdatedEventArgs update in updates)
            {
                if (index.TryGetValue(update.ComponentType, out EcsChangeSubscription[] subscriptions))
                {
                    foreach (EcsChangeSubscription subscription in subscriptions)
                    {
                        subscription.RecordUpdated(update);
                    }
                }
            }
        }

        public void RecordDeleted(ComponentDeletedEventArgs[] deleted)
        {
            Dictionary<Type, EcsChangeSubscription[]> index = Volatile.Read(ref m_subscriptionsByType);
            foreach (IGrouping<IEntity, ComponentDeletedEventArgs> entityGroup in deleted.GroupBy(d => d.Entity))
            {
                ComponentDeletedEventArgs[] components = entityGroup.ToArray();
                HashSet<EcsChangeSubscription> interested = new HashSet<EcsChangeSubscription>();
                foreach (ComponentDeletedEventArgs component in components)
                {
                    if (index.TryGetValue(component.ComponentType, out EcsChangeSubscription[] subscriptions))
                    {
                        interested.UnionWith(subscriptions);
                    }
                }

                foreach (EcsChangeSubscription subscription in interested)
                {
                    if (components[0].EntityDeleted)
                    {
                        subscription.RecordDeleted(new EntityDeletedEventArgs(entityGroup.Key, components));
                    }
                    else
                    {
                        subscription.RecordPartialDeleted(new ComponentRemovedFromEntityEventArgs(entityGroup.Key, components));
                    }
                }
            }
        }

        private void remove(EcsChangeSubscription subscription)
        {
            lock (m_sync)
            {
                publish(m_subscriptions.Where(s => !ReferenceEquals(s, subscription)).ToArray());
            }
        }

        private void publish(EcsChangeSubscription[] subscriptions)
        {
            Dictionary<Type, List<EcsChangeSubscription>> mutableIndex = new();
            foreach (EcsChangeSubscription subscription in subscriptions)
            {
                foreach (Type componentType in subscription.ComponentTypes)
                {
                    if (!mutableIndex.TryGetValue(componentType, out List<EcsChangeSubscription> list))
                    {
                        list = new List<EcsChangeSubscription>();
                        mutableIndex[componentType] = list;
                    }

                    list.Add(subscription);
                }
            }

            Dictionary<Type, EcsChangeSubscription[]> index = mutableIndex.ToDictionary(p => p.Key, p => p.Value.ToArray());
            Volatile.Write(ref m_subscriptions, subscriptions);
            Volatile.Write(ref m_subscriptionsByType, index);
        }
    }

    internal sealed class EcsChangeSubscription : IEcsChangeSubscription
    {
        private readonly object m_sync = new object();
        private readonly Dictionary<Type, ComponentUpdatesAggregator> m_updates;
        private readonly Dictionary<string, EntityDeletedEventArgs> m_deleted = new();
        private readonly Dictionary<string, ComponentRemovedFromEntityEventArgs> m_partialDeleted = new();
        private readonly Action<EcsChangeSubscription> m_onDispose;

        private int m_disposed;

        public EcsChangeSubscription(Type[] componentTypes, Action<EcsChangeSubscription> onDispose)
        {
            ComponentTypes = new HashSet<Type>(componentTypes);
            m_updates = ComponentTypes.ToDictionary(t => t, t => new ComponentUpdatesAggregator(t));
            m_onDispose = onDispose;
        }

        public HashSet<Type> ComponentTypes { get; }

        public CollectorReport Pop()
        {
            lock (m_sync)
            {
                ComponentUpdatedEventArgs[] updated = m_updates.Values.SelectMany(a => a.PopAllMessages()).ToArray();
                EntityDeletedEventArgs[] deleted = m_deleted.Values.OrderBy(e => e.ModifiedDate).ToArray();
                ComponentRemovedFromEntityEventArgs[] partialDeleted = m_partialDeleted.Values.OrderBy(e => e.ModifiedDate).ToArray();

                m_deleted.Clear();
                m_partialDeleted.Clear();

                return createReport(updated, deleted, partialDeleted);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref m_disposed, 1) == 0)
            {
                m_onDispose(this);
            }
        }

        internal void RecordUpdated(ComponentUpdatedEventArgs update)
        {
            if (Volatile.Read(ref m_disposed) != 0)
            {
                return;
            }

            lock (m_sync)
            {
                if (m_updates.TryGetValue(update.ComponentType, out ComponentUpdatesAggregator aggregator))
                {
                    aggregator.Add(update);
                }
            }
        }

        internal void RecordDeleted(EntityDeletedEventArgs deleted)
        {
            if (Volatile.Read(ref m_disposed) != 0)
            {
                return;
            }

            lock (m_sync)
            {
                m_deleted[deleted.Entity.Id] = deleted;
                m_partialDeleted.Remove(deleted.Entity.Id);
            }
        }

        internal void RecordPartialDeleted(ComponentRemovedFromEntityEventArgs deleted)
        {
            if (Volatile.Read(ref m_disposed) != 0)
            {
                return;
            }

            lock (m_sync)
            {
                if (!m_deleted.ContainsKey(deleted.Entity.Id))
                {
                    if (m_partialDeleted.TryGetValue(deleted.Entity.Id, out ComponentRemovedFromEntityEventArgs existing))
                    {
                        ComponentDeletedEventArgs[] components = existing.DeletedComponents
                                                                          .Concat(deleted.DeletedComponents)
                                                                          .GroupBy(c => c.ComponentType)
                                                                          .Select(g => g.Last())
                                                                          .ToArray();
                        deleted = new ComponentRemovedFromEntityEventArgs(deleted.Entity, components);
                    }

                    m_partialDeleted[deleted.Entity.Id] = deleted;
                }
            }
        }

        private static CollectorReport createReport(ComponentUpdatedEventArgs[] updated,
                                                      EntityDeletedEventArgs[] deleted,
                                                      ComponentRemovedFromEntityEventArgs[] partialDeleted)
        {
            EntityUpdatedEventArgs[] entityUpdates = updated.GroupBy(u => u.Entity)
                                                               .Select(g => new EntityUpdatedEventArgs(g.Key, g.ToArray()))
                                                               .ToArray();
            return new CollectorReport
            {
                Updated = entityUpdates,
                Deleted = deleted,
                PartialDeleted = partialDeleted
            };
        }
    }
}
