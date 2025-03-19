namespace EcsSharp
{
    public struct Component
    {
        public static readonly Component Empty = new Component(0);

        public Component(ulong version, object data)
        {
            Version = version;
            Data = data;
        }

        public Component(ulong version) : this() => Version = version;

        public ulong Version { get; set; }
        public object Data { get; set; }

        public override string ToString()
        {
            if (Version == 0)
            {
                return "Empty";
            }

            return $"{nameof(Version)}: {Version}, {Data?.GetType().Name}: {{{Data}}}";
        }
    }
}