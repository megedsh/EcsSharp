using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcsSharp.Distribute;

namespace EcsSharp.Extensions.Json
{
    public class EcsPackageConverter : JsonConverter<EcsPackage>
    {
        private readonly ConcurrentDictionary<string, Type> m_typeCache = new ConcurrentDictionary<string, Type>();

        public override void Write(Utf8JsonWriter writer, EcsPackage value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(nameof(value.Updated));
            JsonSerializer.Serialize(writer, value.Updated, options);

            writer.WritePropertyName(nameof(value.Deleted));
            JsonSerializer.Serialize(writer, value.Deleted, options);

            writer.WritePropertyName(nameof(value.EntityTags));
            JsonSerializer.Serialize(writer, value.EntityTags, options);

            writer.WritePropertyName(nameof(value.DeletedTags));
            JsonSerializer.Serialize(writer, value.DeletedTags, options);

            writer.WriteEndObject();
        }

        public override EcsPackage Read(ref Utf8JsonReader reader,
                                        Type typeToConvert,
                                        JsonSerializerOptions options)
        {
            EcsPackage result = new EcsPackage();
            List<EntityComponentsPair> updated = new List<EntityComponentsPair>();
            Dictionary<string, string[]> entityTags = null;
            while (reader.Read())
            {
                JsonTokenType tokenType = reader.TokenType;
                if (tokenType == JsonTokenType.PropertyName)
                {
                    if (reader.GetString()?.ToLower() == SerializationConst.Updated)
                    {
                        updated = getUpdated(ref reader, options);
                    }

                    else if (reader.GetString()?.ToLower() == SerializationConst.Deleted)
                    {
                        string[] deletedEntities = JsonSerializer.Deserialize<string[]>(ref reader, options);
                        result.AddDeletedEntity(deletedEntities);
                    }

                    else if (reader.GetString()?.ToLower() == SerializationConst.DeletedTags)
                    {
                        string[] deletedTags = JsonSerializer.Deserialize<string[]>(ref reader, options);
                        result.AddDeleteByTag(deletedTags);
                    }

                    else if (reader.GetString()?.ToLower() == SerializationConst.EntityTags)
                    {
                        entityTags = JsonSerializer.Deserialize<Dictionary<string, string[]>>(ref reader, options);
                    }
                }
            }

            if (entityTags == null)
            {
                entityTags = new Dictionary<string, string[]>();
            }

            if (updated.Count > 0)
            {
                foreach (EntityComponentsPair pair in updated)
                {
                    entityTags.TryGetValue(pair.EntityId, out string[] tags);
                    result.AddComponent(pair.EntityId, tags, pair.Components);
                }
            }

            return result;
        }

        private List<EntityComponentsPair> getUpdated(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            List<EntityComponentsPair> result = new List<EntityComponentsPair>();
            while (reader.Read())
            {
                JsonTokenType tokenType = reader.TokenType;
                if (tokenType == JsonTokenType.PropertyName)
                {
                    string entityId = reader.GetString();
                    EntityComponentsPair pair = getEntity(ref reader, entityId, options);
                    if (!pair.IsEmpty)
                    {
                        result.Add(pair);
                    }
                }
                else if (tokenType == JsonTokenType.EndObject)
                {
                    break;
                }
            }

            return result;
        }

        private EntityComponentsPair getEntity(ref Utf8JsonReader reader, string entityId, JsonSerializerOptions options)
        {
            List<Component> l = new List<Component>();
            EntityComponentsPair result = EntityComponentsPair.Empty;
            while (reader.Read())
            {
                JsonTokenType tokenType = reader.TokenType;

                if (tokenType == JsonTokenType.PropertyName)
                {
                    string typeName = reader.GetString();
                    Component component = getComponent(ref reader, typeName, options);
                    if (component.Data != null)
                    {
                        l.Add(component);
                    }
                }

                if (tokenType == JsonTokenType.EndObject)
                {
                    result = new EntityComponentsPair(entityId, l.ToArray());
                    break;
                }
            }

            return result;
        }

        private Component getComponent(ref Utf8JsonReader reader, string typeName, JsonSerializerOptions options)
        {
            Type componentDataType = getType(typeName);
            if (componentDataType == null)
            {
                throw new JsonException($"Failed to find type : {typeName} might be missing a reference to the assembly");
            }

            ulong? version = 0;
            object data = null;
            while (reader.Read())
            {
                JsonTokenType tokenType = reader.TokenType;

                if (tokenType == JsonTokenType.PropertyName && reader.GetString()?.ToLower() == SerializationConst.Version)
                {
                    reader.Read();
                    version = reader.GetUInt64();
                }
                else if (tokenType == JsonTokenType.PropertyName && reader.GetString()?.ToLower() == SerializationConst.Data)
                {
                    reader.Read();
                    data = getData(ref reader, componentDataType, options);
                }

                if (tokenType == JsonTokenType.EndObject)
                {
                    return new Component((ulong)version, data);
                }
            }

            return Component.Empty;
        }

        private object getData(ref Utf8JsonReader reader, Type componentDataType, JsonSerializerOptions options)
        {
            object deserialize = JsonSerializer.Deserialize(ref reader, componentDataType, options);
            return deserialize;
        }

        private Type getType(string typeName)
        {
            if (!m_typeCache.TryGetValue(typeName, out Type result))
            {
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    result = a.GetType(typeName);
                    if (result != null)
                    {
                        break;
                    }
                }

                m_typeCache[typeName] = result;
            }

            return result;
        }
    }
}