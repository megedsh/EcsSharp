using System;

namespace EcsSharp
{
    public struct TypeVersionPair
    {
        public TypeVersionPair(Type type, ulong version)
        {
            Type = type;
            Version = version;
        }

        public Type Type { get; }
        public ulong Version { get; }
    }
}