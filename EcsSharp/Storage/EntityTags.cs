using System.Collections.Generic;

namespace EcsSharp.Storage
{
    public class EntityTags : HashSet<string>
    {
        public EntityTags()
        {
        }

        public EntityTags(IEnumerable<string> collection) : base(collection)
        {
        }

        public override string ToString() => $"[{string.Join(",",this)}]";
    }
}