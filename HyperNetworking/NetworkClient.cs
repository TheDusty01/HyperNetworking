using HyperNetworking.Events;
using HyperNetworking.Messaging;
using HyperNetworking.Messaging.Packets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HyperNetworking.Core
{
    public class NetworkClient
    {
        public void DisconnectDebubg()
        {
            StopRecieve();
            TcpClient = null;
        }

        public static readonly uint ProtocolVersion = 1;

        protected string? host;
        protected int port;
        protected bool isRecieving;

        public bool IsServerClient { get; }
        protected readonly IPacketConverter packetConverter;

        public PacketHandler PacketHandler { get; private set; }

        protected Task? tcpRecieveTask;

        public TcpClient? TcpClient { get; protected set; }
        public NetworkStream NetworkStream { get => TcpClient!.GetStream(); }

        public uint ClientId { get; protected set; } = 0;
        public bool IsFullyConnected { get; internal set; } = false;

        // Keep alive stuff
        public KeepAliveSettings KeepAliveSettings { get; set; }
        public DateTimeOffset LastResponse { get; internal set; }

        // TODO: Use custom event args to include a disconnect reason
        public event EventHandler<ClientEventArgs>? Disconnected;
        public event EventHandler<PacketRecievedEventArgs>? PacketRecieved;

        protected NetworkClient(PacketHandler? packetHandler, IPacketConverter? packetConverter, KeepAliveSettings? keepAliveSettings)
        {
            this.packetConverter = packetConverter ?? PacketHandler.DefaultPacketConverter;
            PacketHandler = packetHandler ?? new PacketHandler(this.packetConverter);
            KeepAliveSettings = keepAliveSettings ?? new KeepAliveSettings();
        }

        internal NetworkClient(TcpClient tcpClient, uint clientId, PacketHandler packetHandler, KeepAliveSettings keepAliveSettings) :
            this(packetHandler, packetHandler.PacketConverter, keepAliveSettings)
        {
            TcpClient = tcpClient;
            ClientId = clientId;
            IsServerClient = true;
            //SetupKeepAlive();   // TODO: Test
        }

        public NetworkClient(string host, ushort port, KeepAliveSettings? keepAliveSettings = null, IPacketConverter? packetConverter = null) :
            this(null, packetConverter, keepAliveSettings)
        {
            this.host = host;
            this.port = port;
            IsServerClient = false;

            PacketHandler.Register<HelloRequestPacket>(OnHelloRequest);

            PacketHandler.Register<KeepAliveRequestPacket>(OnKeepAliveRequest);
            PacketRecieved += NetworkClient_PacketRecieved;
        }

        #region API
        public virtual void Connect()
        {
            if (TcpClient is not null)
                throw new InvalidOperationException("Client is already connected");

            TcpClient = new TcpClient();
            TcpClient.Connect(host, port);
            StartRecieve();
        }

        public virtual void Send<T>(T packet)
        {
            NetworkPacket netPacket = packetConverter.SerializeToPacket(packet);

            BinaryWriter bw = new BinaryWriter(NetworkStream);
            bw.Write(netPacket.Serialize());
            bw.Flush();
        }

        internal virtual void StartRecieve()
        {
            isRecieving = true;
            tcpRecieveTask = Task.Run(Recieve);
        }

        internal virtual void StopRecieve(bool waitForExit = true)
        {
            isRecieving = false;
            if (waitForExit)
                tcpRecieveTask!.Wait();
        }

        protected virtual void Close()
        {
            if (TcpClient is null)
                throw new InvalidOperationException("Client is not connected");

            TcpClient.Close();
            TcpClient = null;
        }

        public virtual void Disconnect()
        {
            StopRecieve();
            Close();

            Disconnected?.Invoke(this, new ClientEventArgs(this));
        }

        //internal virtual bool IsServerClientConnected()
        //{
        //    if (TcpClient is null || !TcpClient.Connected)
        //    {
        //        return false;
        //    }

        //    if (TcpClient.Client.Poll(0, SelectMode.SelectWrite) && (!TcpClient.Client.Poll(0, SelectMode.SelectError)))
        //    {
        //        byte[] buffer = new byte[1];
        //        return TcpClient.Client.Receive(buffer, SocketFlags.Peek) != 0;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

        public virtual bool IsConnected()
        {
            try
            {
                if (TcpClient is null || !TcpClient.Connected)
                    return false;

                /* pear to the documentation on Poll:
                 * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                 * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                 * -or- true if data is available for reading; 
                 * -or- true if the connection has been closed, reset, or terminated; 
                 * otherwise, returns false
                 */
                if (!TcpClient.Client.Poll(0, SelectMode.SelectRead))
                    return true;

                // If the result of Recieve() is != 0, then the client disconnected since poll returned true
                var buffer = new byte[1];
                return TcpClient.Client.Receive(buffer, SocketFlags.Peek) != 0;
            }
            catch (SocketException)
            {
                return false;
            }
        }
        #endregion

        #region Internal
        protected virtual void NetworkClient_PacketRecieved(object sender, PacketRecievedEventArgs e)
        {
            LastResponse = DateTimeOffset.UtcNow;
        }

        protected virtual void OnKeepAliveRequest(NetworkClient client, KeepAliveRequestPacket packet)
        {
            Send(new KeepAliveResponsePacket());
        }

        private void OnHelloRequest(NetworkClient client, HelloRequestPacket packet)
        {
            if (packet.protocolVersion != ProtocolVersion)
                Disconnect();

            ClientId = packet.clientId;
            IsFullyConnected = true;

            Console.WriteLine("OnHelloRequest Id: " + ClientId + " - " + client.ClientId);

            Send(new HelloResponsePacket());
        }

        #region Threads
        private void Recieve()
        {
            BinaryReader br = new BinaryReader(NetworkStream);

            LastResponse = DateTimeOffset.UtcNow;
            DateTimeOffset nextKeepAliveRequest = LastResponse + KeepAliveSettings.Interval;

            while (isRecieving)
            {
                // Socket closed
                if (!IsConnected())
                {
                    Close();
                    Disconnected?.Invoke(this, new ClientEventArgs(this));
                    StopRecieve(false);
                    break;
                }

                // Handle timeouts
                if (KeepAliveSettings.Enabled)
                {
                    if (LastResponse + KeepAliveSettings.Interval > nextKeepAliveRequest)
                    {
                        nextKeepAliveRequest = LastResponse + KeepAliveSettings.Interval;
                    }
                    else if (LastResponse + KeepAliveSettings.TimeoutTime <= DateTimeOffset.UtcNow)
                    {
                        // Timed out (haven't recieved any packet in time)
                        Close();
                        Disconnected?.Invoke(this, new ClientEventArgs(this));
                        StopRecieve(false);
                        break;
                    }
                    
                    if (nextKeepAliveRequest <= DateTimeOffset.UtcNow)
                    {
                        Send(new KeepAliveRequestPacket());
                        nextKeepAliveRequest += KeepAliveSettings.Interval;
                    }
                }

                if (!NetworkStream.DataAvailable)
                    continue;

                NetworkPacket netPacket = new NetworkPacket(packetConverter);
                try
                {
                    netPacket.Read(br);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
                if (!netPacket.IsValid)
                {
                    Console.Error.WriteLine("Couldn't read packet");
                    continue;
                }

                // Invoke event
                PacketRecieved?.Invoke(this, new PacketRecievedEventArgs(netPacket, this));

                // Invoke packet handler
                try
                {
                    // No need to check for is server since the packet handler is either the one from the client or the server
                    PacketHandler.InvokePacket(this, netPacket);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex.Message);
                    Console.Error.WriteLine(ex.StackTrace);
                }
            }

        }
        #endregion
    
        #endregion
    }
}
