using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace EcsSharp.Helpers
{
    static class SerializationHelper
    {
        public static MemoryStream BinarySerializeObject(Object obj)
        {
            MemoryStream ms = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            ms.Position = 0;
            return ms;
        }

        public static T BinaryDeserializeObject<T>(MemoryStream SerializeObject)
        {
            IFormatter formatter = new BinaryFormatter();
            SerializeObject.Position = 0;
            return (T)formatter.Deserialize(SerializeObject);
        }
    }
}
