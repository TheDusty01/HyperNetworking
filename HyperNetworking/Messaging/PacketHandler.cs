using HyperNetworking.Messaging;
using HyperNetworking.Messaging.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace HyperNetworking.Core
{
    public class PacketHandler
    {
        private static IPacketConverter? defaultPacketConverter;
        public static IPacketConverter DefaultPacketConverter
        {
            get
            {
                if (defaultPacketConverter is null)
                    defaultPacketConverter = new JsonPacketConverter();

                return defaultPacketConverter;
            }
        }

        public IPacketConverter PacketConverter { get; }

        private readonly Dictionary<int, PacketInfo> packetIds = new Dictionary<int, PacketInfo>();
        private readonly Dictionary<Type, PacketInfo> packetTypes = new Dictionary<Type, PacketInfo>();

        internal PacketHandler(IPacketConverter packetConverter)
        {
            PacketConverter = packetConverter;
        }

        #region Packets
        public void Register<TPacket>(Action<NetworkClient, TPacket>? handler = null)
        {
            Register(typeof(TPacket), handler is not null ?
                (c, p) => handler(c, (TPacket)p) :
                null);
        }

        public void Register(Type type, Action<NetworkClient, object>? handler = null)
        {
            if (packetTypes.TryGetValue(type, out PacketInfo pi))
            {
                pi.Handler = handler;

                //if (packetTypes.ContainsKey(type))
                //    throw new ArgumentException($"Packet {type.Name} is already registered", nameof(type));

                return;
            }

            PacketInfo packetInfo = new PacketInfo(PacketConverter.GetPacketId(type), type, handler);

            lock (packetIds)
            {
                packetIds.Add(packetInfo.PacketId, packetInfo);
                packetTypes.Add(type, packetInfo);
            }
        }

        public void Unregister<TPacket>()
        {
            Unregister(typeof(TPacket));
        }

        public void Unregister(Type type)
        {
            if (packetTypes.TryGetValue(type, out PacketInfo pi))
            {
                lock (packetIds)
                {
                    packetIds.Remove(pi.PacketId);
                    packetTypes.Remove(type);
                }
            }
        }

        public PacketInfo? GetPacketInfo(object packet)
        {
            return GetPacketInfo(packet.GetType());
        }

        public PacketInfo? GetPacketInfo(Type packetType)
        {
            if (packetTypes.TryGetValue(packetType, out PacketInfo packetInfo))
            {
                return packetInfo;
            }

            return null;
        }

        public PacketInfo? GetPacketInfo(int packetId)
        {
            if (packetIds.TryGetValue(packetId, out PacketInfo packetInfo))
            {
                return packetInfo;
            }

            return null;
        }

        /// <summary>
        /// Invokes a handler for the specified packet.
        /// </summary>
        /// <param name="networkClient"></param>
        /// <param name="netPacket"></param>
        /// <returns><see langword="true"/> if the packet was registered, <see langword="false"/> if the packet wasn't registered.</returns>
        /// <exception cref="InvalidDataException"></exception>
        internal bool InvokePacket(NetworkClient networkClient, NetworkPacket netPacket)
        {
            PacketInfo? packetInfo = GetPacketInfo(netPacket.PacketId);
            if (packetInfo is null)
                return false;

            object? packet = netPacket.Deserialize(packetInfo.Type);
            if (packet is null)
                throw new InvalidDataException("Couldn't deserialize the packet");

            packetInfo.Handler?.Invoke(networkClient, packet);
            return true;
        }
        #endregion

        public class PacketInfo
        {
            public int PacketId { get; set; }
            public Type Type { get; set; }
            public Action<NetworkClient, object>? Handler { get; set; }

            public PacketInfo(int packetId, Type type, Action<NetworkClient, object>? handler = null)
            {
                PacketId = packetId;
                Type = type;
                Handler = handler;
            }
        }


    }
}
