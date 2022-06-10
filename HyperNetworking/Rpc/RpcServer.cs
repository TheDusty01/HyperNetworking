using HyperNetworking.Messaging;
using HyperNetworking.Messaging.Packets;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HyperNetworking.Core
{
    public class RpcServer : NetworkServer<RpcClient>, IRpcNetworkParticipant
    {
        public ServerRpcEventManager ServerRpc { get; private set; }
        public RpcEventManager Rpc => ServerRpc;

        public RpcServer(IPAddress address, ushort port, KeepAliveSettings? keepAliveSettings = null, IPacketConverter? packetConverter = null) : base(address, port, keepAliveSettings, packetConverter)
        {
            ServerRpc = new ServerRpcEventManager(this);

            PacketHandler.Register<RpcRequestPacket>(OnRpcRequest);
            PacketHandler.Register<RpcResponsePacket>(OnRpcResponse);
        }

        #region API
        public override void Start()
        {
            if (isListening)
                throw new InvalidOperationException("Server is already listening");

            Rpc.Setup();
            tcpListener.Start();

            // Start listening task
            isListening = true;
            listenerTask = Task.Run(Listen);
        }
        #endregion

        #region Internal
        private void OnRpcRequest(NetworkClient client, RpcRequestPacket packet)
        {
            var result = Rpc.CallEvent(packet.EventName, packet.Args);
            client.Send(new RpcResponsePacket { requestId = packet.RequestId, returnValue = result.ReturnValue, exception = result.Exception });
        }

        private void OnRpcResponse(NetworkClient client, RpcResponsePacket packet)
        {
            Rpc.RecieveResponse(client, packet);
        }

        protected override RpcClient AcceptClient(TcpClient tcpClient, uint clientId)
        {
            return new RpcClient(tcpClient, clientId, PacketHandler, KeepAliveSettings, Rpc);
        }
        #endregion
    }
}
