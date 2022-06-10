using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HyperNetworking.Messaging
{
    public interface IPacketConverter
    {
        /// <summary>
        /// Convert an object to a byte array.
        /// </summary>
        /// <param name="obj">Object to be converted</param>
        /// <returns>An byte array which represents the given object</returns>
        byte[] Serialize(object obj);

        /// <summary>
        /// Convert an byte array to an object of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Destination type for the converted binary data</typeparam>
        /// <param name="data">Binary data to be converted</param>
        /// <returns>An object of <typeparamref name="T"/></returns>
        T? Deserialize<T>(byte[] data);

        /// <summary>
        /// Convert a byte array to an object.
        /// </summary>
        /// <param name="type">Destination type for the converted binary data</param>
        /// <param name="data">Binary data to be converted</param>
        /// <returns>An object</returns>
        object? Deserialize(Type type, byte[] data);

        /// <summary>
        /// Convert an object to a <see cref="NetworkPacket"/>.
        /// </summary>
        /// <typeparam name="T">Source type of the supplied object</typeparam>
        /// <param name="obj">Object to be converted</param>
        /// <returns>A <see cref="NetworkPacket"/> which represents the <paramref name="obj"/></returns>
        NetworkPacket SerializeToPacket<T>(T obj);

        /// <summary>
        /// Convert the data of a <see cref="NetworkPacket"/> to an object of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Destination type for the converted binary data</typeparam>
        /// <param name="netPacket">Binary data to be converted</param>
        /// <returns>An object of <typeparamref name="T"/></returns>
        T? DeserializeToPacket<T>(NetworkPacket netPacket);

        /// <summary>
        /// Generates an unique id for the specified type.
        /// </summary>
        /// <typeparam name="T">Type of the packet</typeparam>
        /// <returns>A unique <see cref="int"/> for <typeparamref name="T"/></returns>
        int GetPacketId<T>();

        /// <summary>
        /// Generates an unique id for the specified type.
        /// </summary>
        /// <param name="type">Type of the packet</param>
        /// <returns>A unique <see cref="int"/> for <paramref name="type"/></returns>
        int GetPacketId(Type type);
    }
}
