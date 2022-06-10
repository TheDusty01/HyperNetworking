using System;
using System.Collections.Generic;
using System.Text;

namespace HyperNetworking.Messaging.Packets
{
    [Packet]
    public class HelloRequestPacket
    {
        public uint protocolVersion;
        public uint clientId;
    }
}