using HyperNetworking.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace HyperNetworking.Events
{
    public class ClientEventArgs : EventArgs
    {
        internal ClientEventArgs(NetworkClient client)
        {
            Client = client;
        }

        /// <summary>
        /// Modifying this instance will can cause problems.
        /// </summary>
        public NetworkClient Client { get; }
    }

    public class ClientConnectingEventArgs : ClientEventArgs
    {
        internal ClientConnectingEventArgs(NetworkClient client) : base(client)
        {
        }

        /// <summary>
        /// Setting this to true will disconnect the client.
        /// </summary>
        public bool Reject { get; set; } = false;
    }
}
