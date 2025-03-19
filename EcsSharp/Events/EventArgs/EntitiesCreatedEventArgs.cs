using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EcsSharp.Events.EventArgs
{
    public class EntitiesCreatedEventArgs : IReadOnlyCollection<EntityCreatedEventArgs>
    {
        private readonly Dictionary<string, EntityCreatedEventArgs> m_map;

        public EntitiesCreatedEventArgs(IList<EntityCreatedEventArgs> list)
        {
            m_map = list.ToDictionary(e => e.Entity.Id, e => e);
        }

        public bool TryGetEventForEntity(string id, out EntityCreatedEventArgs entityUpdatedEventArgs) => m_map.TryGetValue(id, out entityUpdatedEventArgs);

        public bool TryGetEventForEntity(IEntity entity, out EntityCreatedEventArgs entityUpdatedEventArgs) => m_map.TryGetValue(entity.Id, out entityUpdatedEventArgs);

        IEnumerator<EntityCreatedEventArgs> IEnumerable<EntityCreatedEventArgs>.GetEnumerator() => m_map.Values.GetEnumerator();

        public IEnumerator GetEnumerator() => m_map.Values.GetEnumerator();
        public int Count => m_map.Values.Count;
    }
}