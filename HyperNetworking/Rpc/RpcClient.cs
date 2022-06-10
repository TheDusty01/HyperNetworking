using HyperNetworking.Messaging;
using HyperNetworking.Messaging.Packets;
using System;
using System.Net.Sockets;

namespace HyperNetworking.Core
{
    public class RpcClient : NetworkClient, IRpcNetworkParticipant
    {
        public RpcEventManager Rpc { get; private set; }

        internal RpcClient(TcpClient tcpClient, uint clientId, PacketHandler packetHandler, KeepAliveSettings keepAliveSettings, RpcEventManager rpc) :
            base(tcpClient, clientId, packetHandler, keepAliveSettings)
        {
            Rpc = rpc;
        }

        public RpcClient(string host, ushort port, KeepAliveSettings? keepAliveSettings = null, IPacketConverter? packetConverter = null) :
            base(host, port, keepAliveSettings, packetConverter)
        {
            Rpc = new ClientRpcEventManager(this);

            PacketHandler.Register<RpcRequestPacket>(OnRpcRequest);
            PacketHandler.Register<RpcResponsePacket>(OnRpcResponse);
        }

        #region API
        public override void Connect()
        {
            if (TcpClient != null)
                throw new InvalidOperationException("Client is already connected");

            Rpc.Setup();
            TcpClient = new TcpClient();
            TcpClient.Connect(host, port);
            StartRecieve();
        }
        #endregion

        #region Internal
        private void OnRpcRequest(NetworkClient client, RpcRequestPacket packet)
        {
            var result = Rpc.CallEvent(packet.EventName, packet.Args);
            Send(new RpcResponsePacket { requestId = packet.RequestId, returnValue = result.ReturnValue, exception = result.Exception });
        }

        private void OnRpcResponse(NetworkClient client, RpcResponsePacket packet)
        {
            Rpc!.RecieveResponse(client, packet);
        }
        #endregion
    }
}