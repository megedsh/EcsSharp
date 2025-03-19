using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EcsSharp.Events.EventArgs
{
    public class EntitiesUpdatedEventArgs : IReadOnlyCollection<EntityUpdatedEventArgs>
    {
        private readonly Dictionary<string, EntityUpdatedEventArgs> m_map;

        public EntitiesUpdatedEventArgs(IList<EntityUpdatedEventArgs> list)
        {
            m_map = list.ToDictionary(e => e.Entity.Id, e => e);
        }

        public bool TryGetEventForEntity(string id, out EntityUpdatedEventArgs entityUpdatedEventArgs) => m_map.TryGetValue(id, out entityUpdatedEventArgs);

        public bool TryGetEventForEntity(IEntity entity, out EntityUpdatedEventArgs entityUpdatedEventArgs) => m_map.TryGetValue(entity.Id, out entityUpdatedEventArgs);

        IEnumerator<EntityUpdatedEventArgs> IEnumerable<EntityUpdatedEventArgs>.GetEnumerator() => m_map.Values.GetEnumerator();

        public IEnumerator GetEnumerator() => m_map.Values.GetEnumerator();
        public int Count => m_map.Values.Count;
    }
}