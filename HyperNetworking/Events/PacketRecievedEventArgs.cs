using HyperNetworking.Core;
using HyperNetworking.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace HyperNetworking.Events
{
    public class PacketRecievedEventArgs : EventArgs
    {
        internal PacketRecievedEventArgs(NetworkPacket networkPacket, NetworkClient client)
        {
            NetworkPacket = networkPacket;
            Client = client;
        }

        /// <summary>
        /// Modifying this instance will alter the behaviour of packet handling.
        /// </summary>
        public NetworkPacket NetworkPacket { get; }

        /// <summary>
        /// Modifying this instance will can cause problems.
        /// </summary>
        public NetworkClient Client { get; }
    }
}
