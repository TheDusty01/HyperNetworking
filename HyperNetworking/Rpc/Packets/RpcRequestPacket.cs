using System;
using System.Collections.Generic;
using System.Text;

namespace HyperNetworking.Messaging.Packets
{
    [Packet]
    class RpcRequestPacket
    {
        public Guid RequestId { get; }
        public string EventName { get; }
        public object[] Args { get; }

        public RpcRequestPacket(Guid requestId, string eventName, object[] args)
        {
            RequestId = requestId;
            EventName = eventName;
            Args = args;
        }

        //public bool hasResult;
    
        
    }

}
