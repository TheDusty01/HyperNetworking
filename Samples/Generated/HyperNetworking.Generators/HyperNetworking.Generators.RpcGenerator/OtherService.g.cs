// Auto-generated code
using System;
using HyperNetworking.Core;
using HyperNetworking.Messaging;

namespace Samples
{
    public partial class OtherService
    {

		public void OtherServiceSharedSharedRpc(Samples.Test t1, System.Boolean execLocally)
		{
			if (execLocally)
			{
				Console.WriteLine("Local exec: OtherServiceSharedRpc");
				OtherServiceShared(t1);
				return;
			}

			Console.WriteLine("Remote exec: OtherServiceSharedRpc");
			string name = RpcEventManager.GetEventName("Samples.OtherService", "System.Void OtherServiceShared(Samples.Test)");
			Rpc.SendRequest(name, t1);
		}

		public void OtherServiceCientClientRpc(Samples.Test t1, params System.UInt32[] clientIds)
		{
			if (IsLocal(false))
			{
				Console.WriteLine("Local exec: OtherServiceCientRpc");
				OtherServiceCient(t1);
				return;
			}

			Console.WriteLine("Remote exec: OtherServiceCientRpc");
			string name = RpcEventManager.GetEventName("Samples.OtherService", "System.Void OtherServiceCient(Samples.Test)");
			((ServerRpcEventManager)Rpc).SendRequest(clientIds, name, t1);
		}

		public global::Samples.Test OtherServiceServerServerRpc(Samples.Test t1, Samples.Test t2)
		{
			if (IsLocal(true))
			{
				Console.WriteLine("Local exec: OtherServiceServerRpc");
				return OtherServiceServer(t1, t2);
			}

			Console.WriteLine("Remote exec: OtherServiceServerRpc");
			string name = RpcEventManager.GetEventName("Samples.OtherService", "Samples.Test OtherServiceServer(Samples.Test, Samples.Test)");
			return Rpc.SendRequest<global::Samples.Test>(name, t1, t2).Result;
		}

		public global::System.Threading.Tasks.Task<global::Samples.Test> OtherServiceServerTaskServerRpc(Samples.Test t1, Samples.Test t2)
		{
			if (IsLocal(true))
			{
				Console.WriteLine("Local exec: OtherServiceServerTaskRpc");
				return OtherServiceServerTask(t1, t2);
			}

			Console.WriteLine("Remote exec: OtherServiceServerTaskRpc");
			string name = RpcEventManager.GetEventName("Samples.OtherService", "System.Threading.Tasks.Task OtherServiceServerTask(Samples.Test, Samples.Test)");
			return Rpc.SendRequest<global::Samples.Test>(name, t1, t2);
		}

		public async global::System.Threading.Tasks.Task<global::Samples.Test> OtherServiceServerTaskAsyncServerRpc(Samples.Test t1, Samples.Test t2)
		{
			if (IsLocal(true))
			{
				Console.WriteLine("Local exec: OtherServiceServerTaskAsyncRpc");
				return await OtherServiceServerTaskAsync(t1, t2);
			}

			Console.WriteLine("Remote exec: OtherServiceServerTaskAsyncRpc");
			string name = RpcEventManager.GetEventName("Samples.OtherService", "System.Threading.Tasks.Task OtherServiceServerTaskAsync(Samples.Test, Samples.Test)");
			return await Rpc.SendRequest<global::Samples.Test>(name, t1, t2);
		}

		public global::System.Threading.Tasks.Task OtherServiceServerNoReturnTaskServerRpc(Samples.Test t1, Samples.Test t2)
		{
			if (IsLocal(true))
			{
				Console.WriteLine("Local exec: OtherServiceServerNoReturnTaskRpc");
				return OtherServiceServerNoReturnTask(t1, t2);
			}

			Console.WriteLine("Remote exec: OtherServiceServerNoReturnTaskRpc");
			string name = RpcEventManager.GetEventName("Samples.OtherService", "System.Threading.Tasks.Task OtherServiceServerNoReturnTask(Samples.Test, Samples.Test)");
			return Rpc.SendRequest(name, t1, t2);
		}

		public async global::System.Threading.Tasks.Task OtherServiceServerNoReturnTaskAsyncServerRpc(Samples.Test t1, Samples.Test t2)
		{
			if (IsLocal(true))
			{
				Console.WriteLine("Local exec: OtherServiceServerNoReturnTaskAsyncRpc");
				await OtherServiceServerNoReturnTaskAsync(t1, t2);
				return;
			}

			Console.WriteLine("Remote exec: OtherServiceServerNoReturnTaskAsyncRpc");
			string name = RpcEventManager.GetEventName("Samples.OtherService", "System.Threading.Tasks.Task OtherServiceServerNoReturnTaskAsync(Samples.Test, Samples.Test)");
			await Rpc.SendRequest(name, t1, t2);
		}

		public void OtherServiceServerNoReturnServerRpc(Samples.Test t1, Samples.Test t2)
		{
			if (IsLocal(true))
			{
				Console.WriteLine("Local exec: OtherServiceServerNoReturnRpc");
				OtherServiceServerNoReturn(t1, t2);
				return;
			}

			Console.WriteLine("Remote exec: OtherServiceServerNoReturnRpc");
			string name = RpcEventManager.GetEventName("Samples.OtherService", "System.Void OtherServiceServerNoReturn(Samples.Test, Samples.Test)");
			Rpc.SendRequest(name, t1, t2);
		}

	}
}
