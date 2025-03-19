using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using EcsSharp.Distribute;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EcsSharp.Extensions.Newtonsoft
{
    public class EcsPackageConverter : JsonConverter<EcsPackage>
    {
        private readonly ConcurrentDictionary<string, Type> m_typeCache = new ConcurrentDictionary<string, Type>();
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, EcsPackage value, JsonSerializer serializer)
        {
        }

        public override EcsPackage ReadJson(JsonReader reader,
                                            Type objectType,
                                            EcsPackage existingValue,
                                            bool hasExistingValue,
                                            JsonSerializer serializer)
        {
            EcsPackage result = new EcsPackage();
            List<EntityComponentsPair> updated = new List<EntityComponentsPair>();
            Dictionary<string, string[]> entityTags = null;

            while (reader.Read())
            {
                JsonToken tokenType = reader.TokenType;
                object tokenValue = reader.Value;
                Type valueType = reader.ValueType;
                if (tokenType != JsonToken.PropertyName || tokenValue == null || valueType != typeof(string) )
                {
                    continue;
                }

                switch (tokenValue.ToString().ToLower())
                {
                    case SerializationConst.Updated:
                        updated = getUpdated(reader, serializer);
                        break;

                    case SerializationConst.Deleted:
                        reader.Read();
                        string[] deletedEntities = serializer.Deserialize<string[]>(reader);
                        result.AddDeletedEntity(deletedEntities);
                        break;

                    case SerializationConst.DeletedTags:
                        reader.Read();
                        string[] deletedTags = serializer.Deserialize<string[]>(reader);
                        result.AddDeleteByTag(deletedTags);
                        break;
                    
                    case SerializationConst.EntityTags:
                        reader.Read();
                        entityTags = serializer.Deserialize<Dictionary<string, string[]>>(reader);
                        break;
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

        private List<EntityComponentsPair> getUpdated(JsonReader reader, JsonSerializer jsonSerializer)
        {
            List<EntityComponentsPair> result = new List<EntityComponentsPair>();
            while (reader.Read())
            {
                JsonToken tokenType = reader.TokenType;
                object tokenValue = reader.Value;
                Type valueType = reader.ValueType;

                if (tokenType == JsonToken.PropertyName && valueType == typeof(string))
                {
                    EntityComponentsPair pair = getEntity(reader, (string)tokenValue, jsonSerializer);
                    if (!pair.IsEmpty)
                    {
                        result.Add(pair);
                    }
                }

                if (tokenType == JsonToken.EndObject)
                {
                    break;
                }
            }

            return result;
        }

        private EntityComponentsPair getEntity(JsonReader reader, string entityId, JsonSerializer jsonSerializer)
        {
            List<Component> l = new List<Component>();
            EntityComponentsPair result = EntityComponentsPair.Empty;
            while (reader.Read())
            {
                JsonToken tokenType = reader.TokenType;
                object tokenValue = reader.Value;
                Type valueType = reader.ValueType;

                if (tokenType == JsonToken.PropertyName && valueType == typeof(string))
                {
                    Component component = getComponent(reader, (string)tokenValue, jsonSerializer);
                    if (component.Data != null)
                    {
                        l.Add(component);
                    }
                }

                if (tokenType == JsonToken.EndObject)
                {
                    result = new EntityComponentsPair(entityId, l.ToArray());
                    break;
                }
            }

            return result;
        }

        private Component getComponent(JsonReader reader, string typeName, JsonSerializer jsonSerializer)
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
                JsonToken tokenType = reader.TokenType;
                object tokenValue = reader.Value;
                Type valueType = reader.ValueType;
                if (tokenValue != null && tokenType == JsonToken.PropertyName && valueType == typeof(string) && tokenValue.ToString().ToLower() == SerializationConst.Version)
                {
                    string readAsString = reader.ReadAsString();
                    if (!ulong.TryParse(readAsString, out ulong ver))
                    {
                        throw new JsonException($"Failed to parse '{readAsString}'to type ulong (Version)");
                    }
                    version = ver;
                }
                else if (tokenValue != null && tokenType == JsonToken.PropertyName && valueType == typeof(string) && tokenValue.ToString().ToLower() == SerializationConst.Data)
                {
                    data = getData(reader, componentDataType, jsonSerializer);
                }

                if (tokenType == JsonToken.EndObject)
                {
                    return new Component(version ?? 0, data);
                }
            }

            return Component.Empty;
        }

        private object getData(JsonReader reader, Type componentDataType, JsonSerializer jsonSerializer)
        {
            reader.Read();
            if (reader.TokenType == JsonToken.StartArray)
            {
                JArray jArray = JArray.Load(reader);
                JsonReader arrayReader = jArray.CreateReader();
                return jsonSerializer.Deserialize(arrayReader, componentDataType);
            }

            JObject jObject = JObject.Load(reader);
            JsonReader jsonReader = jObject.CreateReader();
            return jsonSerializer.Deserialize(jsonReader, componentDataType);
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