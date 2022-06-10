// Auto-generated code
using System;
using HyperNetworking.Core;
using HyperNetworking.Messaging;

namespace Samples
{
    public partial class MathUtils
    {

		public void DoSomethingSharedSharedRpc(Samples.Test t1, System.Boolean execLocally)
		{
			if (execLocally)
			{
				Console.WriteLine("Local exec: DoSomethingSharedRpc");
				DoSomethingShared(t1);
				return;
			}

			Console.WriteLine("Remote exec: DoSomethingSharedRpc");
			string name = RpcEventManager.GetEventName("Samples.MathUtils", "System.Void DoSomethingShared(Samples.Test)");
			Rpc.SendRequest(name, t1);
		}

		public string DoSomethingSharedReturnSharedRpc(Samples.Test t1, System.Boolean execLocally)
		{
			if (execLocally)
			{
				Console.WriteLine("Local exec: DoSomethingSharedReturnRpc");
				return DoSomethingSharedReturn(t1);
			}

			Console.WriteLine("Remote exec: DoSomethingSharedReturnRpc");
			string name = RpcEventManager.GetEventName("Samples.MathUtils", "System.String DoSomethingSharedReturn(Samples.Test)");
			return Rpc.SendRequest<string>(name, t1).Result;
		}

		public void DoSomethingClientClientRpc(Samples.Test t1, params System.UInt32[] clientIds)
		{
			if (IsLocal(false))
			{
				Console.WriteLine("Local exec: DoSomethingClientRpc");
				DoSomethingClient(t1);
				return;
			}

			Console.WriteLine("Remote exec: DoSomethingClientRpc");
			string name = RpcEventManager.GetEventName("Samples.MathUtils", "System.Void DoSomethingClient(Samples.Test)");
			((ServerRpcEventManager)Rpc).SendRequest(clientIds, name, t1);
		}

		public void DoSomethingServerServerRpc(Samples.Test t1)
		{
			if (IsLocal(true))
			{
				Console.WriteLine("Local exec: DoSomethingServerRpc");
				DoSomethingServer(t1);
				return;
			}

			Console.WriteLine("Remote exec: DoSomethingServerRpc");
			string name = RpcEventManager.GetEventName("Samples.MathUtils", "System.Void DoSomethingServer(Samples.Test)");
			Rpc.SendRequest(name, t1);
		}

	}
}
