using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using EcsSharp.Events.EventArgs;

namespace EcsSharp.Events;

public class ComponentRemovedFromEntityEventArgs : EntityEventArgs<ComponentDeletedEventArgs>
{
    public ComponentRemovedFromEntityEventArgs(IEntity entity, ComponentDeletedEventArgs[] deletedComponents):base(entity,deletedComponents)
    {
    }

    public ComponentDeletedEventArgs[] DeletedComponents => ComponentEventArgs;
}

public class ComponentsRemovedFromEntitiesEventArgs : IReadOnlyCollection<ComponentRemovedFromEntityEventArgs>
{
    private readonly Dictionary<string, ComponentRemovedFromEntityEventArgs> m_map;
    public ComponentsRemovedFromEntitiesEventArgs(IList<ComponentRemovedFromEntityEventArgs> list)
    {
        m_map = list.ToDictionary(e => e.Entity.Id, e => e);
    }

    public bool TryGetEventForEntity(string id, out ComponentRemovedFromEntityEventArgs entityPartiallyDeleted) => m_map.TryGetValue(id, out entityPartiallyDeleted);

    public bool TryGetEventForEntity(IEntity entity, out ComponentRemovedFromEntityEventArgs entityPartiallyDeleted) => m_map.TryGetValue(entity.Id, out entityPartiallyDeleted);

    IEnumerator<ComponentRemovedFromEntityEventArgs> IEnumerable<ComponentRemovedFromEntityEventArgs>.GetEnumerator() => m_map.Values.GetEnumerator();

    public IEnumerator GetEnumerator() => m_map.Values.GetEnumerator();
    public int Count => m_map.Values.Count;
}

public class ComponentsRemovedFromEntitiesEvent: DelegateWrapper<ComponentsRemovedFromEntitiesEventArgs>
{
    public static ComponentsRemovedFromEntitiesEvent operator +(ComponentsRemovedFromEntitiesEvent d, Action<ComponentsRemovedFromEntitiesEventArgs> callback) => (ComponentsRemovedFromEntitiesEvent)d.AddCallback(callback);

    public static ComponentsRemovedFromEntitiesEvent operator -(ComponentsRemovedFromEntitiesEvent d, Action<ComponentsRemovedFromEntitiesEventArgs> callback) => (ComponentsRemovedFromEntitiesEvent)d.RemoveCallback(callback);
}