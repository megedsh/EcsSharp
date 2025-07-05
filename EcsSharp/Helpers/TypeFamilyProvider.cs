using System;
using System.Collections.Generic;
using System.Linq;

namespace EcsSharp.Helpers
{
    public interface ITypeFamilyProvider
    {
        IEnumerable<Type> GetTypeFamily(Type type);
    }
    public class AllInterfacesFamilyProvider: ITypeFamilyProvider
    {
        public IEnumerable<Type> GetTypeFamily(Type type)
        {            
            Type[] interfaces = type.GetInterfaces();
            return new[] { type }.Concat(interfaces);
        }
    }

    public class ExplicitInterfacesFamilyProvider : ITypeFamilyProvider
    {
        private readonly Dictionary<Type, List<Type>> m_masterList  = new();
        private          Dictionary<Type, Type[]>     m_readonlyMap = new();
        private readonly object                       m_sync        = new object();

        public void Add(Type type, params Type[] interfaces)
        {
            lock(m_sync)
            {
                m_masterList.ComputeIfAbsent(type, _ => new List<Type>()).AddRange(interfaces);
                m_readonlyMap = null;
            }
        }

        public IEnumerable<Type> GetTypeFamily(Type type)
        {
            Dictionary<Type, Type[]> map = getMap();
            if (map.TryGetValue(type, out Type[] interfaces))
            {
                return new[] { type }.Concat(interfaces);
            }

            return [type];
        }

        private Dictionary<Type, Type[]> getMap()
        {
            if (m_readonlyMap != null)
            {
                return m_readonlyMap;
            }

            lock(m_sync)
            {
                if (m_readonlyMap == null)
                {
                    m_readonlyMap = m_masterList.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
                }

                return m_readonlyMap;
            }
        }
    }
}