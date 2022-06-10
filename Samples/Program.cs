using HyperNetworking;
using HyperNetworking.Core;
using HyperNetworking.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Samples
{
    public class Test : IEquatable<Test>
    {
        public static readonly Test First = new Test(1, "First req", 1);
        public static readonly Test Second = new Test(2, "Second req", 2);
        public static readonly Test Third = new Test(3, "Third req", 3);


        public static readonly Test Res1 = new Test(1, "First result", 1);
        public static readonly Test Res2 = new Test(2, "Second result", 2);
        public static readonly Test Res3 = new Test(3, "Third result", 3);

        public Test(int someInt, string someText, byte someByte)
        {
            this.someInt = someInt;
            this.someText = someText;
            this.someByte = someByte;
        }

        public int someInt;
        public string someText;
        public byte someByte;

        public override bool Equals(object obj)
        {
            return Equals(obj as Test);
        }

        public bool Equals(Test other)
        {
            if (other is null)
                return false;

            return someInt == other.someInt && someText == other.someText && someByte == other.someByte;
        }
    }

    public partial class OtherService : RpcService
    {
        [ServerRpc]
        public Test OtherServiceServer(Test t1, Test t2)
        {
            Console.WriteLine("OtherServiceServer did something");
            return Test.Res1;
        }

        [ServerRpc]
        public Task<Test> OtherServiceServerTask(Test t1, Test t2)
        {
            Console.WriteLine("OtherServiceServerTask did something");
            return Task.FromResult(Test.Res1);
        }

        [ServerRpc]
        public async Task<Test> OtherServiceServerTaskAsync(Test t1, Test t2)
        {
            await Task.Yield();
            Console.WriteLine("OtherServiceServerTaskAsync did something");
            return Test.Res2;
        }

        [ServerRpc]
        public Task OtherServiceServerNoReturnTask(Test t1, Test t2)
        {
            Console.WriteLine("OtherServiceServerNoReturnTask did something");
            return Task.CompletedTask;
        }

        [ServerRpc]
        public async Task OtherServiceServerNoReturnTaskAsync(Test t1, Test t2)
        {
            Console.WriteLine("OtherServiceServerNoReturnTaskAsync did something");
            await Task.CompletedTask;
        }

        [ServerRpc]
        public void OtherServiceServerNoReturn(Test t1, Test t2)
        {
            Console.WriteLine("OtherServiceServerNoReturn did something");
        }

        [ClientRpc]
        public void OtherServiceCient(Test t1)
        {
            Console.WriteLine("OtherServiceCient did something");
        }

        [SharedRpc]
        public void OtherServiceShared(Test t1)
        {
            Console.WriteLine("OtherServiceShared did something");
            //OtherServiceCLIENTRpc(t1);
        }
    }

    public partial class MathUtils : RpcService
    {
        protected int Age { private get; set; }
        public int age;

        [ServerRpc]
        public void DoSomethingServer(Test t1)
        {
            Console.WriteLine("SERVER did something");
        }

        [ServerRpc]
        public void DoSomething()
        {
            Console.WriteLine("SERVER did something");
        }

        [ClientRpc]
        public void DoSomethingElse(Test t1)
        {
            Console.WriteLine("CLIENT did something: " + t1.someText);
        }

        [SharedRpc]
        public Test ExecuteWithReturn(Test t1)
        {
            Console.WriteLine("Anyone did something: " + t1.someText);
            return new Test(1337, "I was gonna tell you a joke about UDP.. but you might not get it.", 69);
        }

        [ServerRpc]
        public async Task ExecuteSomethingAsync()
        {
            Console.WriteLine("Server ExecuteSomethingAsync");
            await Task.Yield();
        }

        [ServerRpc]
        public async Task<Test> ExecuteSomethingAsyncWithReturn()
        {
            await Task.Yield();
            Console.WriteLine("Server ExecuteSomethingAsync");
            return new Test(1337, "What did the router say to the doctor? It hurts when IP.", 69);
        }

        [ClientRpc]
        public void DoSomethingClient(Test t1)
        {
            Console.WriteLine("CLIENT did something");
        }

        [SharedRpc]
        public void DoSomethingShared(Test t1)
        {
            Console.WriteLine("BOTH did something NO RETURN!!");
        }

        public void DoSomethingSharedApi(Test t1)
        {
            // injected code:
            // if (!ILocal())
            // {
            //      Get Target client ids (if shared or if client)
            //      Send request
            // }

            var method = GetType().GetMethod("DoSomethingShared", new Type[] { typeof(Test) });
            //Server.Rpc.SendRequest(RpcEventManager.GetEventName(GetType(), method), t1);
            Rpc.SendRequest(RpcEventManager.GetEventName(GetType(), method), t1);
        }

        public void DoSomethingSharedApi2(Test t1)
        {
            var type = GetType();
            var method = type.GetMethod("DoSomethingShared", new Type[] { typeof(Test) });
            Rpc.SendRequest(RpcEventManager.GetEventName(type, method), t1);

            //// [62 17 - 62 38]
            //IL_0000: ldarg.0      // this
            //IL_0001: call         instance class [System.Runtime]System.Type [System.Runtime]System.Object::GetType()
            //IL_0006: stloc.0      // 'type'

            //// [63 17 - 63 95]
            //IL_0007: ldloc.0      // 'type'
            //IL_0008: ldstr        "DoSomethingShared"
            //IL_000d: ldc.i4.1
            //IL_000e: newarr       [System.Runtime]System.Type
            //IL_0013: dup
            //IL_0014: ldc.i4.0
            //IL_0015: ldtoken      Samples.Program/Test
            //IL_001a: call         class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
            //IL_001f: stelem.ref
            //IL_0020: callvirt     instance class [System.Runtime]System.Reflection.MethodInfo [System.Runtime]System.Type::GetMethod(string, class [System.Runtime]System.Type[])
            //IL_0025: stloc.1      // 'method'

            //// [64 17 - 64 88]
            //IL_0026: ldsfld       class [HyperNetworking]HyperNetworking.Core.NetworkServer Samples.Program::server
            //IL_002b: callvirt     instance class [HyperNetworking]HyperNetworking.Core.RpcEventManager [HyperNetworking]HyperNetworking.Core.NetworkServer::get_Rpc()
            //IL_0030: ldloc.0      // 'type'
            //IL_0031: ldloc.1      // 'method'
            //IL_0032: call         string [HyperNetworking]HyperNetworking.Core.RpcEventManager::GetEventName(class [System.Runtime]System.Type, class [System.Runtime]System.Reflection.MethodInfo)
            //IL_0037: ldc.i4.1
            //IL_0038: newarr       [System.Runtime]System.Object
            //IL_003d: dup
            //IL_003e: ldc.i4.0
            //IL_003f: ldarg.1      // t1
            //IL_0040: stelem.ref
            //IL_0041: callvirt     instance class [System.Runtime]System.Threading.Tasks.Task [HyperNetworking]HyperNetworking.Core.RpcEventManager::SendRequest(string, object[])
            //IL_0046: pop

            //// [65 13 - 65 14]
            //IL_0047: ret
        }

        [SharedRpc]
        public string DoSomethingSharedReturn(Test t1)
        {
            Console.WriteLine("BOTH did something");
            return "testiiii";
        }

        public string DoSomethingSharedReturnApi(Test t1)
        {
            Console.WriteLine("Request started!");
            var method = GetType().GetMethod("DoSomethingSharedReturn", new Type[] { typeof(Test) });
            //string res = Server.Rpc.SendRequest<string>(RpcEventManager.GetEventName(GetType(), method), t1).Result;
            string res = Rpc.SendRequest<string>(RpcEventManager.GetEventName(GetType(), method), t1).Result;
            Console.WriteLine("Request done!");
            return res;
        }

    }

    class Program
    {
        public static RpcServer server;
        public static RpcClient client;

        struct EmptyStruct
        {

        }
        static async void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            //new Serialization().Run();
            //return;

            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new EmptyStruct()));

            server = new RpcServer(IPAddress.Any, 25565, new KeepAliveSettings(true, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)), new JsonPacketConverter());
            server.PacketRecieved += Server_PacketRecieved;
            server.PacketHandler.Register<Test>((c, t) =>
            {
                Console.WriteLine("Recieved Server: ");
                Console.WriteLine(JsonConvert.SerializeObject(t));
            });
            server.Start();

            Console.WriteLine("Started");
            //Thread.Sleep(2000);

            client = new RpcClient("127.0.0.1", 25565, new KeepAliveSettings(true, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)), new JsonPacketConverter());
            client.PacketRecieved += Client_PacketRecieved;
            client.PacketHandler.Register<Test>((c, t) =>
            {
                Console.WriteLine("Recieved Client: ");
                Console.WriteLine(JsonConvert.SerializeObject(t));
            });
            client.Connect();


            Thread.Sleep(1000);
            server.Send(new Test(15, "My Text", 255));
            //client.Send(new Test(187, "My Text  aaaa", 45));

            var mathUtilsServer = server.Rpc.GetService<MathUtils>();
            var mathUtilsClient = client.Rpc.GetService<MathUtils>();

            bool execLocally = false;
            mathUtilsClient.ExecuteWithReturnSharedRpc(new Test(0, "3 SQL statements walk into a NoSQL bar. Soon, they walk out. They couldn't find a table.", 0), execLocally);

            await mathUtilsClient.ExecuteSomethingAsyncServerRpc();
            var returnValue = await mathUtilsClient.ExecuteSomethingAsyncWithReturnServerRpc();

            var otherServiceServer = server.Rpc.GetService<OtherService>();
            var otherServiceClient = client.Rpc.GetService<OtherService>();


            // Local test
            //otherServiceServer.OtherServiceServerNoReturnServerRpc(Test.First, Test.First);
            //Test resServer = otherServiceServer.OtherServiceServerRpc(Test.First, Test.First);
            //PrintRes(true, resServer, Test.Res1);

            // Remote test
            //otherServiceClient.OtherServiceServerNoReturnServerRpc(Test.First, Test.Third);
            Test resClient = otherServiceClient.OtherServiceServerServerRpc(Test.First, Test.First);
            PrintRes(true, resClient, Test.Res1);

            // Shared test
            //otherServiceClient.OtherServiceSharedSharedRpc(Test.First, true);
            //Test resClient = otherServiceClient.OtherServiceServerServerRpc(Test.First, Test.First);
            //PrintRes(true, resClient, Test.Res1);

            server.ClientDisconnected += Server_ClientDisconnected;
            client.Disconnected += Client_Disconnected;

            Thread.Sleep(500);

            Console.WriteLine("Trying to disconnect..");
            //client.Disconnect();
            //client.DisconnectDebubg();
            //client = null;

            //server.Stop();
            //server.StopDebug();
            //server = null;

            Console.WriteLine("Disconnect executed");

            
            // Keep console alive
            Console.ReadKey(true);
        }


        private static void Client_Disconnected(object sender, HyperNetworking.Events.ClientEventArgs e)
        {
            Console.WriteLine("[Client] Client disconnected");
        }

        private static void Server_ClientDisconnected(object sender, HyperNetworking.Events.ClientEventArgs e)
        {
            Console.WriteLine("[Server] Client disconnected");
        }

        public static readonly Random Random = new Random();
        public static int GetRandomDelay(int max)
        {
            lock (Random)
                return 500 + Random.Next(max);
        }

        private static void Server_PacketRecieved(object sender, HyperNetworking.Events.PacketRecievedEventArgs e)
        {
            //Console.WriteLine("Recieved Server: " + e.NetworkPacket.PacketId);
            //Console.WriteLine(JsonConvert.SerializeObject(PacketConverter.Deserialize<Test>(e.NetworkPacket.Data)));
        }

        private static void Client_PacketRecieved(object sender, HyperNetworking.Events.PacketRecievedEventArgs e)
        {
            Console.WriteLine("Recieved Client: " + e.NetworkPacket.PacketId);

            var data = e.NetworkPacket.Deserialize<Test>();
            if (data is not null)
            {
                Console.WriteLine("JSON representation of object: " + JsonConvert.SerializeObject(data));
            }
        }

        public static void PrintRes<T>(bool executedOnServer, T data, T expected)
        {
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            string expectedJson = JsonConvert.SerializeObject(expected, Formatting.Indented);

            Console.WriteLine($"Got response from {(executedOnServer ? "SERVER" : "CLIENT")}:");
            Console.WriteLine(json);
            if (data.Equals(expected))
            {
                Console.WriteLine("Response is as expected: ✔️");
            }
            else
            {
                Console.WriteLine("Response is NOT as expected: ❌");
                Console.WriteLine("Expected:");
                Console.WriteLine(expectedJson);
            }
        }
    }
}
