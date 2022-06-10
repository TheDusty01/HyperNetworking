using HyperNetworking.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HyperNetworking.Messaging
{
    public abstract class RpcService
    {
        //    public NetworkServer Server { get; internal set; }
        //    public NetworkClient Client { get; internal set; }

        [AllowNull]
        public RpcEventManager Rpc { get; internal set; }
        public bool IsServer { get; internal set; }   // Is this instance an rpc server

        public bool IsLocal(bool fromServerRpc)
        {
            return (fromServerRpc && IsServer) ||
                (!fromServerRpc && !IsServer);
        }
    }
}
