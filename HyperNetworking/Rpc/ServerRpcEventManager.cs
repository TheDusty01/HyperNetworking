using HyperNetworking.Messaging;
using HyperNetworking.Messaging.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HyperNetworking.Core
{
    public class ServerRpcEventManager : RpcEventManager<ServerRpcAttribute, ClientRpcAttribute>
    {
        private RpcServer Server => (RpcServer)networkParticipant;

        public ServerRpcEventManager(RpcServer networkParticipant) : base(networkParticipant)
        {
        }

        #region Request/Response handling
        public override Task SendRequest(string eventName, params object[] args)
        {
            return SendRequest(Server.GetAllClientIds(), eventName, args);
        }

        public Task SendRequest(uint[] targetClientIds, string eventName, params object[] args)
        {
            if (!DoesRemoteEventExist(eventName))
                throw new ArgumentException($"Remote event does not exist: {eventName}", nameof(eventName));
            if (targetClientIds.Length == 0)
                targetClientIds = Server.GetAllClientIds();

            RpcRequestPacket req = new RpcRequestPacket
            (
                Guid.NewGuid(),
                eventName,
                args
                //hasResult = true
            );

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            pendingRequest.Add(req.RequestId, new PendingRpcRequest(targetClientIds, res =>
            {
                if (res.Exception != null)
                    tcs.SetException(res.Exception);
                else
                    tcs.SetResult(true);
            }));

            // Send request
            Server.Send(req, targetClientIds);

            // TODO: Set exception after timeout
            //tcs.SetException(new Exception("Timeout"));

            return tcs.Task;
        }

        public override Task<TResult> SendRequest<TResult>(string eventName, params object[] args)
        {
            return SendRequest<TResult>(Server.GetAllClientIds(), eventName, args);
        }

        public Task<TResult> SendRequest<TResult>(uint[] targetClientIds, string eventName, params object[] args)
        {
            if (!DoesRemoteEventExist(eventName))
                throw new ArgumentException($"Remote event does not exist: {eventName}", nameof(eventName));

            RpcRequestPacket req = new RpcRequestPacket
            (
                Guid.NewGuid(),
                eventName,
                args
                //hasResult = true
            );

            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            pendingRequest.Add(req.RequestId, new PendingRpcRequest(targetClientIds, res =>
            {
                if (res.Exception != null)
                    tcs.SetException(res.Exception);
                else
                    tcs.SetResult(res.GetReturnValue<TResult>()!);
            }));

            // Send request
            Server.Send(req, targetClientIds);

            // TODO: Set exception after timeout
            //tcs.SetException(new Exception("Timeout"));

            return tcs.Task;
        }

        internal override void RecieveResponse(NetworkClient client, RpcResponsePacket resp)
        {
            if (!pendingRequest.TryGetValue(resp.requestId, out PendingRpcRequest pendingReq))
                throw new Exception($"Request id {resp.requestId} is not pending");

            // Ack checks
            if (!pendingReq.CanAcknowledge(client.ClientId))
                throw new Exception($"Client {client.ClientId} is trying to acknowledge request id {resp.requestId} without permission!");
            if (pendingReq.HasAcknowledged(client.ClientId))
                throw new Exception($"Client {client.ClientId} is trying to acknowledge request id {resp.requestId} twice!");

            // Acknowledge and wait for all clients to acknowledge
            pendingReq.Acknowledge(client.ClientId);
            if (!pendingReq.IsAcknowledged())
                return;

            pendingReq.action(new RpcResult(resp.returnValue, resp.exception));

            // Remove pending request
            pendingRequest.Remove(resp.requestId);
        }
        #endregion
    }
}
