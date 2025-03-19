namespace EcsSharp.Extensions.Json
{
    internal readonly struct EntityComponentsPair
    {
        public static EntityComponentsPair Empty = new EntityComponentsPair(null,null);
        public EntityComponentsPair(string entityId, Component[] components)
        {
            EntityId = entityId;
            Components = components;
        }
        public string EntityId { get; }
        public Component[] Components { get; }
        public bool IsEmpty => EntityId == null;
    }
}