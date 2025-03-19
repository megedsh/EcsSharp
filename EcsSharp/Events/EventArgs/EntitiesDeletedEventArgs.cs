using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EcsSharp.Events.EventArgs
{
    public class EntitiesDeletedEventArgs : IReadOnlyCollection<EntityDeletedEventArgs>
    {
        private readonly Dictionary<string, EntityDeletedEventArgs> m_map;

        public EntitiesDeletedEventArgs(IList<EntityDeletedEventArgs> list)
        {
            m_map = list.ToDictionary(e => e.Entity.Id, e => e);
        }

        public bool TryGetEventForEntity(string id, out EntityDeletedEventArgs entityUpdatedEventArgs) => m_map.TryGetValue(id, out entityUpdatedEventArgs);

        public bool TryGetEventForEntity(IEntity entity, out EntityDeletedEventArgs entityUpdatedEventArgs) => m_map.TryGetValue(entity.Id, out entityUpdatedEventArgs);

        IEnumerator<EntityDeletedEventArgs> IEnumerable<EntityDeletedEventArgs>.GetEnumerator() => m_map.Values.GetEnumerator();

        public IEnumerator GetEnumerator() => m_map.Values.GetEnumerator();
        public int Count => m_map.Values.Count;
    }
}