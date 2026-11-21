using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using EcsSharp.StructComponents;

namespace EcsSharp.Extensions.Json
{
    public class StructComponentJsonConverter:JsonConverter<IStructComponent>
    {
        private static readonly string s_dataName = nameof(IStructComponent<object>.Data);

        public override bool CanConvert(Type typeToConvert)
        {
            if (typeof(IStructComponent).IsAssignableFrom(typeToConvert))
            {
                return true;
            }

            return base.CanConvert(typeToConvert);
        }

        public override IStructComponent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if(typeToConvert.IsAbstract)
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

            object value = JsonSerializer.Deserialize(ref reader,dataType, options);

            methodInfo.Invoke(instance, [value]);

            return (IStructComponent)instance;

        }

        public override void Write(Utf8JsonWriter writer, IStructComponent value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.UntypedData, options);
        }
    }
}