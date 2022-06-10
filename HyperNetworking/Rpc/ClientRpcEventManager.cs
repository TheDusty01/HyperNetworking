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
    public class ClientRpcEventManager : RpcEventManager<ClientRpcAttribute, ServerRpcAttribute>
    {
        internal ClientRpcEventManager(RpcClient networkParticipant) : base(networkParticipant)
        {
        }

        #region Request/Response handling
        public override Task SendRequest(string eventName, params object[] args)
        {
            if (!DoesRemoteEventExist(eventName))
                throw new ArgumentException($"Remote event does not exist: {eventName}", nameof(eventName));

            RpcRequestPacket req = new RpcRequestPacket(
                Guid.NewGuid(),
                eventName,
                args
            );

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            pendingRequest.Add(req.RequestId, new PendingRpcRequest(res =>
            {
                if (res.Exception != null)
                    tcs.SetException(res.Exception);
                else
                    tcs.SetResult(true);
            }));

            // Send request
            networkParticipant.Send(req);

            // TODO: Set exception after timeout
            //tcs.SetException(new Exception("Timeout"));

            return tcs.Task;
        }

        public override Task<TResult> SendRequest<TResult>(string eventName, params object[] args)
        {
            if (!DoesRemoteEventExist(eventName))
                throw new ArgumentException($"Remote event does not exist: {eventName}", nameof(eventName));

            RpcRequestPacket req = new RpcRequestPacket(
                Guid.NewGuid(),
                eventName,
                args
                //hasResult = true
            );

            TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();
            pendingRequest.Add(req.RequestId, new PendingRpcRequest(res =>
            {
                if (res.Exception != null)
                    tcs.SetException(res.Exception);
                else
                    tcs.SetResult(res.GetReturnValue<TResult>()!);
            }));

            // Send request
            networkParticipant.Send(req);

            // TODO: Set exception after timeout
            //tcs.SetException(new Exception("Timeout"));

            return tcs.Task;
        }

        internal override void RecieveResponse(NetworkClient client, RpcResponsePacket resp)
        {
            if (!pendingRequest.TryGetValue(resp.requestId, out PendingRpcRequest pendingReq))
                throw new Exception($"Request id {resp.requestId} is not pending");

            if (client.IsServerClient)
                throw new Exception($"Client {client.ClientId} is trying to acknowledge request id {resp.requestId} instead of the server!");

            pendingReq.action(new RpcResult(resp.returnValue, resp.exception));

            // Remove pending request
            pendingRequest.Remove(resp.requestId);
        }
        #endregion
    }
}
