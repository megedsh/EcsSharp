﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace EcsSharp
{
    internal class EntityCollection : IEntityCollection
    {
        public static readonly EntityCollection Empty = new EntityCollection(Array.Empty<IEntity>());

        private readonly List<IEntity> m_entities;

        public int Count => m_entities.Count;

        IEnumerator IEnumerable.GetEnumerator() => m_entities.GetEnumerator();

        public EntityCollection(IEnumerable<IEntity> entities) => m_entities = new List<IEntity>(entities);

        public IEnumerator<IEntity> GetEnumerator() => m_entities.GetEnumerator();

        public IEntity this[int index]
        {
            get => m_entities[index];
            set => m_entities[index] = value;
        }

        public IEntityCollection Clone()
        {            
            return new EntityCollection(m_entities.Select(e => e.Clone()));
        }

        public override string ToString()
        {
            return $"{nameof(Count)}: {Count}";
        }
    }
}