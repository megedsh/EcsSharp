using System.Collections.Generic;

namespace EcsSharp.Helpers
{
    public class EntityBuilder
    {
        private readonly EcsObjectPool m_ecsObjectPool;
        private readonly EcsRepo       m_ecsRepo;

        private readonly List<string> m_tags       = new List<string>();
        private readonly List<object> m_components = new List<object>();
        private          string       m_id;

        internal EntityBuilder(EcsObjectPool ecsObjectPool, EcsRepo ecsRepo)
        {
            m_ecsObjectPool = ecsObjectPool;
            m_ecsRepo = ecsRepo;
        }

        public EntityBuilder WithId(string id)
        {
            m_id = id;
            return this;
        }

        public EntityBuilder WithTags(params string[] tags)
        {
            m_tags.AddRange(tags);
            return this;
        }

        public EntityBuilder WithComponents(params object[] components)
        {
            m_components.AddRange(components);
            return this;
        }

        public IEntity Build()
        {
            IEntity entity = m_ecsRepo.Create(m_components.ToArray(), m_tags.ToArray(), m_id);
            reset();
            m_ecsObjectPool.Release(this);
            return entity;
        }

        private void reset()
        {
            m_id = null;
            m_components.Clear();
            m_tags.Clear();
        }
    }
}