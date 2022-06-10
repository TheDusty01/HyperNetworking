using System;
using System.Collections.Generic;
using System.Text;

namespace HyperNetworking.Messaging.Packets
{
    [Packet]
    class RpcResponsePacket
    {
        public Guid requestId;
        public object? returnValue;
        public Exception? exception;
    }
}
