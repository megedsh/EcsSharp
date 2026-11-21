using System;
using System.Reflection;

using EcsSharp.StructComponents;

using Newtonsoft.Json;

namespace EcsSharp.Extensions.Newtonsoft
{
    public class StructComponentJsonConverter : JsonConverter<IStructComponent>
    {
        private static readonly string s_dataName = nameof(IStructComponent<object>.Data);

        public override void WriteJson(JsonWriter writer, IStructComponent value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.UntypedData);
        }

        public override IStructComponent ReadJson(JsonReader reader, Type typeToConvert, IStructComponent existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (typeToConvert.IsAbstract)
            {
                throw new NotSupportedException("Cannot deserialize abstract types");
            }

            object instance = Activator.CreateInstance(typeToConvert);

            PropertyInfo propertyInfo = typeToConvert.GetProperty(s_dataName);
            if (propertyInfo == null)
            {
                throw new NotSupportedException($"Cannot deserialize - Failed to find property : '{s_dataName}' ");
            }

            Type dataType = propertyInfo.GetMethod.ReturnType;
            MethodInfo methodInfo = propertyInfo.SetMethod;

            object value = serializer.Deserialize(reader, dataType);

            methodInfo.Invoke(instance, [value]);

            return (IStructComponent)instance;
        }
    }
}