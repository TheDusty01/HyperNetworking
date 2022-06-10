using System;
using System.Collections.Generic;
using System.Text;

namespace HyperNetworking.Core
{
    public interface IRpcNetworkParticipant
    {
        RpcEventManager Rpc { get; }
        void Send<T>(T packet);

    }
}
