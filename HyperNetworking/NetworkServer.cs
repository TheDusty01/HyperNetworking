using HyperNetworking.Events;
using HyperNetworking.Messaging;
using HyperNetworking.Messaging.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HyperNetworking.Core
{
    public abstract class NetworkServer<TClient> where TClient : NetworkClient
    {
        public void StopDebug()
        {
            // Cancel listener task
            isListening = false;
            tcpListener.Stop();
            clients.ForEach(c => c.DisconnectDebubg());
            //tcpListener.Stop();
        }

        public static readonly uint ProtocolVersion = 1;

        protected readonly IPAddress address;
        protected readonly int port;

        protected readonly TcpListener tcpListener;
        //private readonly UdpClient udpClient;

        public IPacketConverter PacketConverter { get; init; }
        public PacketHandler PacketHandler { get; private set; }

        protected bool isListening = false;

        protected Task? listenerTask;

        // TODO: Convert list of clients to dictionary to drastically improve lookup speed
        protected readonly List<TClient> clients = new List<TClient>();

        public KeepAliveSettings KeepAliveSettings { get; set; }

        public event EventHandler<ClientConnectingEventArgs>? ClientConnecting;
        public event EventHandler<ClientEventArgs>? ClientConnected;
        public event EventHandler<ClientEventArgs>? ClientDisconnected;

        public event EventHandler<PacketRecievedEventArgs>? PacketRecieved;

        public NetworkServer(IPAddress address, ushort port, KeepAliveSettings? keepAliveSettings = null, IPacketConverter? packetConverter = null)
        {
            this.address = address;
            this.port = port;

            PacketConverter = packetConverter ?? PacketHandler.DefaultPacketConverter;
            PacketHandler = new PacketHandler(PacketConverter);
            KeepAliveSettings = keepAliveSettings ?? new KeepAliveSettings();

            tcpListener = new TcpListener(address, port);

            PacketHandler.Register<HelloResponsePacket>(OnHelloResponse);

            PacketHandler.Register<KeepAliveRequestPacket>(OnKeepAliveRequest);
            PacketRecieved += NetworkServer_PacketRecieved;
        }

        #region API
        public virtual void Start()
        {
            if (isListening)
                throw new InvalidOperationException("Server is already listening");

            tcpListener.Start();
            //SetupKeepAlive();   // TODO: Test

            // Start listening task
            isListening = true;
            listenerTask = Task.Run(Listen);
        }

        public virtual void Stop()
        {
            if (!isListening)
                throw new InvalidOperationException("Server is not listening");

            // Cancel listener task
            isListening = false;
            tcpListener.Stop();
            listenerTask!.Wait();

            // Disconnect all clients
            lock (clients)
            {
                foreach (var client in clients)
                {
                    client.Disconnected -= NetClient_Disconnected;
                    client.Disconnect();
                }
                clients.Clear();
            }
        }

        public virtual uint[] GetAllClientIds()
        {
            uint[] ids = new uint[clients.Count];
            for (int i = 0; i < clients.Count; i++)
            {
                ids[i] = clients[i].ClientId;
            }

            return ids;
        }

        public virtual void Send<T>(T packet)
        {
            NetworkPacket netPacket = PacketConverter.SerializeToPacket(packet);
            byte[] packetData = netPacket.Serialize();

            foreach (var client in clients)
            {
                BinaryWriter bw = new BinaryWriter(client.NetworkStream);
                bw.Write(packetData);
                bw.Flush();
                Console.WriteLine("Sent");  // TODO: Remove
            }
        }

        public virtual void Send<T>(T packet, uint[] clientIds)
        {
            if (clientIds.Length == 0)
            {
                // Use faster loop
                Send(packet);
                return;
            }

            NetworkPacket netPacket = PacketConverter.SerializeToPacket(packet);
            byte[] packetData = netPacket.Serialize();

            foreach (var client in clients)
            {
                if (clientIds.Contains(client.ClientId))
                {
                    BinaryWriter bw = new BinaryWriter(client.NetworkStream);
                    bw.Write(packetData);
                    bw.Flush();
                    Console.WriteLine("Sent to " + client.ClientId);
                    return;
                }
            }
        }
        #endregion

        #region Internal
        private void NetworkServer_PacketRecieved(object sender, PacketRecievedEventArgs e)
        {
            e.Client.LastResponse = DateTimeOffset.UtcNow;
        }

        protected virtual void OnKeepAliveRequest(NetworkClient client, KeepAliveRequestPacket packet)
        {
            Send(new KeepAliveResponsePacket());
        }

        private void OnHelloResponse(NetworkClient client, HelloResponsePacket packet)
        {
            Console.WriteLine("OnHelloResponse: " + client.ClientId);
            client.IsFullyConnected = true;
        }

        protected int currentClientId = 1;
        protected virtual uint GetNextClientId()
        {
            return (uint)Interlocked.Increment(ref currentClientId);
        }

        protected abstract TClient AcceptClient(TcpClient tcpClient, uint clientId);

        #region Threads
        protected virtual void Listen()
        {
            while (isListening)
            {
                try
                {
                    TClient netClient = AcceptClient(tcpListener.AcceptTcpClient(), GetNextClientId());

                    var clientConnectingEventArgs = new ClientConnectingEventArgs(netClient);
                    ClientConnecting?.Invoke(this, clientConnectingEventArgs);

                    // Check if client should be rejected
                    if (clientConnectingEventArgs.Reject)
                    {
                        netClient.Disconnect();
                        continue;
                    }
                    
                    clients.Add(netClient);

                    // Add eventhandler
                    netClient.Disconnected += NetClient_Disconnected;
                    if (PacketRecieved != null)
                        netClient.PacketRecieved += PacketRecieved;

                    netClient.StartRecieve();

                    ClientConnected?.Invoke(this, new ClientEventArgs(netClient));

                    // TODO: Move to event Send hello
                    netClient.Send(new HelloRequestPacket { protocolVersion = ProtocolVersion, clientId = netClient.ClientId });
                }
                catch (InvalidOperationException ex)
                {
                    // Listener has not been started yet
                    Console.Error.WriteLine(ex.Message);
                    Console.Error.WriteLine(ex.StackTrace);
                }
                catch (SocketException ex)
                {
                    // Socket closed
                    SocketError code = ex.SocketErrorCode;
                    Console.WriteLine("Socket closed");
                }
            }
        }

        protected virtual void NetClient_Disconnected(object sender, ClientEventArgs e)
        {
            lock (clients)
                clients.Remove((TClient)e.Client);

            ClientDisconnected?.Invoke(this, e);
        }
        #endregion

        #endregion
    }

    public class NetworkServer : NetworkServer<NetworkClient>
    {
        public NetworkServer(IPAddress address, ushort port, KeepAliveSettings? keepAliveSettings = null, IPacketConverter? packetConverter = null) :
            base(address, port, keepAliveSettings, packetConverter)
        {
        }

        protected override NetworkClient AcceptClient(TcpClient tcpClient, uint clientId)
        {
            return new NetworkClient(tcpClient, clientId, PacketHandler, KeepAliveSettings);
        }
    }
}
