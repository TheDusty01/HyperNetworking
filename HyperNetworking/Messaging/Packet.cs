using System;
using System.Collections.Generic;
using System.Text;

namespace HyperNetworking.Messaging
{
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class PacketAttribute : Attribute
    {

    }

    //public interface IPacket
    //{
    //    public int PacketId { get; }
    //}

    //public class BasePacket : IPacket
    //{
    //    private int packetId = -1;

    //    public int PacketId
    //    {
    //        get
    //        {
    //            if (packetId == -1)
    //                packetId = GetType().GetHashCode();

    //            return packetId;
    //        }
    //    }
    //}
}
