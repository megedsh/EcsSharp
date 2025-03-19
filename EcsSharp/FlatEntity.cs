using System.Collections.Generic;

namespace EcsSharp
{
    public class FlatEntity
    {
        public string Id { get; set;}
        public string[] Tags { get; set;}
        public Dictionary<string, object> Components { get; set;}

        public FlatEntity(string id, string[] tags, Dictionary<string, object> components)
        {
            Id = id;
            Tags = tags;
            Components = components;
        }

        public FlatEntity()
        {
            Components = new Dictionary<string, object>();
        }

        public void Clear()
        {
            Components?.Clear();
            Tags = null;
            Id = null;
        }

        protected bool Equals(FlatEntity other) => Id == other.Id;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((FlatEntity)obj);
        }

        public override int GetHashCode() => Id != null ? Id.GetHashCode() : 0;
    }
}