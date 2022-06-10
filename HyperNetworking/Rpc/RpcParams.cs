using System;
using System.Collections.Generic;
using System.Text;

namespace HyperNetworking.Messaging
{
    public struct RpcParams
    {
        uint[] targetClientsIds;
        uint fromClientId;
    }
}
