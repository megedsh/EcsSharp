using System;
using System.Collections.Generic;
using System.Linq;

namespace EcsSharp.Helpers
{
    public static class TypeFamilyProvider
    {
        public static IEnumerable<Type> GetTypeFamily(Type type)
        {
            //Review: What about base classes ?
            Type[] interfaces = type.GetInterfaces();
            return new[] { type }.Concat(interfaces);
        }
    }
}
