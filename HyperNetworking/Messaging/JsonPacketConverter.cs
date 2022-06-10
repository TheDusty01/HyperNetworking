using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HyperNetworking.Messaging
{
    public class JsonPacketConverter : IPacketConverter
    {
        private static readonly JsonSerializerSettings settings = new()
        {
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto
        };

        /// <inheritdoc />
        public byte[] Serialize(object obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, settings));
        }

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] data)
        {
            if (data.Length == 0)
                return default;

            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data), settings);
        }

        /// <inheritdoc />
        public object? Deserialize(Type type, byte[] data)
        {
            if (data.Length == 0)
                return default;

            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), type, settings);
        }

        /// <inheritdoc />
        public NetworkPacket SerializeToPacket<T>(T obj)
        {
            //byte[] data = Serialize(obj!);
            //return new NetworkPacket(this)
            //{
            //    PacketId = GetPacketId<T>(),
            //    Data = data,
            //    DataLength = data.Length
            //};
            return new NetworkPacket(this, typeof(T), obj!);
        }

        /// <inheritdoc />
        public T? DeserializeToPacket<T>(NetworkPacket netPacket)
        {
            return netPacket.Deserialize<T>();
        }

        /// <inheritdoc />
        public int GetPacketId<T>()
        {
            return typeof(T).GetHashCode();
        }

        /// <inheritdoc />
        public int GetPacketId(Type type)
        {
            return type.GetHashCode();
        }
    }
}
