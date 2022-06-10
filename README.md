# HyperNetworking
A TCP networking library with different abstraction layers, keep alive and RPC support based on Unitiy's MLAPI.

Due to the lack of time many important features and goals of this library aren't supported yet as it is nowhere near ready for production.\
I've decided to publish the code anyway as eventually someone may use this repo for educational purposes, reference or other things.

## Missing/Planned features
- Whole rewrite/overhaul and better API design
- UDP support
- Support any kind of streams over the network, not only packets
- Built-in binary serializers
- Separate the ``NetworkClient`` class into one class for a local client and into a class which describes a connected client of the server
- Cleanup the source generator
- DI support for RPC services
- Adding the client/server object whihch called the method to the RPC method

## Setup
Clone this repository.

The current state of the library wouldn't even be considered as an alpha therefor shouldn't be used at all which is why it isn't on NuGet even as a pre-release.

## Usage
There are multiple ways of recieving and sending data over the network using this library.

The following part shows multiple ways on sending and recieving packets. The RPC part has it's own section below this one.

### Prerequisites
First of you need to initialize a server and or a client:
```cs
// Initialize a server listening on any interface on port 42069
var server = new NetworkServer(IPAddress.Any, 42069);
server.Start();
// Initialize a client which will connect to localhost on port 42069
var client = client = new NetworkClient("127.0.0.1", 42069);
client.Connect();
```
You can even specify the keep alive settings and packet converter:
```cs
// The server sends a keep alive packet to the client(s) if no message was recieved for 5 seconds.
// If the client fails to respond in 10 seconds the client recieves a timeout and gets disconnected.
var server = new RpcServer(IPAddress.Any, 42069,
    new KeepAliveSettings(true, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
    new JsonPacketConverter()
);
```

The following examples use this sample class:
```cs
public class Test
{
    public Test(int someInt, string someText, byte someByte)
    {
        this.someInt = someInt;
        this.someText = someText;
        this.someByte = someByte;
    }
}
```

### Events
Shared events:
```cs
client.PacketRecieved += PacketRecieved;
static void PacketRecieved(object sender, HyperNetworking.Events.PacketRecievedEventArgs e)
{
    Console.WriteLine("Recieved Packet Id: " + e.NetworkPacket.PacketId);

    var data = e.NetworkPacket.Deserialize<Test>();
    if (data is not null)
    {
        Console.WriteLine("JSON representation of Test object: " + JsonConvert.SerializeObject(data));
    }
}
```
Server events:
```cs
// ClientConnecting event
server.ClientConnecting += Server_ClientConnecting;
static void Server_ClientConnecting(object sender, HyperNetworking.Events.ClientConnectingEventArgs e)
{
    Console.WriteLine("Client tries to connect: " + client.ClientId);
    // Disconnect the client
    e.Reject = true;
}

// ClientConnected event
server.ClientConnected += Server_ClientConnected;
static void Server_ClientConnected(object sender, HyperNetworking.Events.ClientEventArgs e)
{
    Console.WriteLine("Client connected: " + client.ClientId);
}

// ClientDisconnected event
server.ClientDisconnected += Server_ClientDisconnected;
static void Server_ClientDisconnected(object sender, HyperNetworking.Events.ClientEventArgs e)
{
    Console.WriteLine("Client disconnected: " + client.ClientId);
}
```
Client events:
```cs
// Disconnected event
client.Disconnected += Client_Disconnected;
static void Client_Disconnected(object sender, HyperNetworking.Events.ClientEventArgs e)
{
    Console.WriteLine("Disconnected: " + e.Client.ClientId);
}
```

### Sending packets
Send a packet to every connected client:
```cs
server.Send(new Test(15, "My Text", 255));
// Gets executed immediately after all packets were sent, does NOT wait for the response
Console.WriteLine("Data got sent!");
```
Send a packet to specific connected clients:
```cs
// Send data to client with id 0 and client with id 5
server.Send(new Test(15, "My Text", 255), 0, 5);
```

### Recieving packets
You can use the packet recieved event shown before or register the packet to the packet handler:
```cs
// Register a handler for the Test packet which only executes once
server.PacketHandler.Register<Test>((c, t) =>
{
    Console.WriteLine("Recieved Test packet, printing as JSON:");
    Console.WriteLine(JsonConvert.SerializeObject(t));

    // Unregister the handler
    server.PacketHandler.Unregister<Test>();
});
```
**Note:** There can only be 1 handler for each type per server/client instance.

## RPC Usage
The big advantage of the RPC (Remote Procedure Call) API is that you can use a shared code base for your client and server. As mentioned before it is based on Unity's MLAPI and designed to be as easy as possible. That means calling a method on the remote is as simple as calling a normal method.

Unity's MLAPI generates custom method bodies and wraps the original method during runtime so the user doesn't need to do it himself, similiar to what SignalR does under the hood. This was my first approach aswell but I quickly decided to Source Generators instead. The reason behind that is simple: Source Generators are way easier to debug than dynamic IL generation during runtime, the user can see what's happening under the hood, more flexibility which menas the user can implement the network code on his own if there needs to be some custom logic.

### Prerequisites
First of you need to initialize a server and or a client:
```cs
// Initialize a server listening on any interface on port 42069
var server = new RpcServer(IPAddress.Any, 42069);
server.Start();
// Initialize a client which will connect to localhost on port 42069
var client = client = new RpcClient("127.0.0.1", 42069);
client.Connect();
```

The following examples use this sample class:
```cs
public class Test
{
    public Test(int someInt, string someText, byte someByte)
    {
        this.someInt = someInt;
        this.someText = someText;
        this.someByte = someByte;
    }
}
```

### Services
First of all you need to register a service which contains your RPC methods.

It's pretty simple:
```cs
public partial class MathUtilsService : RpcService {}
```
That's all, the service get's registered automatically upon creation of an RpcServer and or RpcClient.

If the assembly containing the service gets loaded after the creation you can also manually register the service:
```cs
var mathUtilsFromClient = client.Rpc.AddService<MathUtilsService>(new MathUtilsService());
```

Just do the following to retrieve services:
```cs
var mathUtilsFromClient = client.Rpc.GetService<MathUtilsService>();
// Same for the server
var mathUtilsFromServer = server.Rpc.GetService<MathUtilsService>();
```
**Note:** Each service is singleton per client/server. This means if you have multiple clients running in the same project, each client has the different instance of the service class.

### Defining and calling RPC methods
Now we need to add methods to call. In this example I've added a method which prints something and can only get executed on the server. 
```cs
[ServerRpc]
public void DoSomething()
{
    Console.WriteLine("SERVER did something");
}
```
To call the method now it is as simple as this:
```cs
mathUtilsFromClient.DoSomethingServerRpc();
// Waits until the method executed on the remote, can take up several seconds, depending on what the keep alive timeout is set to
Console.WriteLine("Executed!");

// We can also execute it locally from the server
mathUtilsFromServer.DoSomethingServerRpc();
// This would be the same as calling the method directly
mathUtilsFromServer.DoSomething();
```
As you can tell that's not the same method! The method we're calling has the suffix of ``ServerRpc`` at the end. That happens because this method gets generated.

Now if we add another method with parameters on the client:
```cs
[ClientRpc]
public void DoSomethingElse(Test t1)
{
    Console.WriteLine("CLIENT did something: " + t1.someText);
}
```
You would call it like the other one, except here the suffix being ``ClientRpc`` cause the method is marked as a client method and therefor can only be called by the server.
```cs
mathUtilsFromServer.DoSomethingClientRpc(new Test(420, "Dusty was here", 69));

// You can also execute the method on specific clients only
// This would execute the method on client with id 0 and client with 5
mathUtilsFromServer.DoSomethingClientRpc(
    new Test(420, "Dusty was here", 69),
    0, 5
);
// Executes when both clients finished executing
Console.WriteLine("Executed on client 0 and 5!");
```
As many parameters as normal methods can have are supported.

There are also shared methods which can be called by both the client and server. In this case the method also returns something: 
```cs
[SharedRpc]
public Test ExecuteWithReturn(Test t1)
{
    Console.WriteLine("Anyone did something: " + t1.someText);
    return new Test(1337, "I was gonna tell you a joke about UDP.. but you might not get it.", 69);
}

// Calling the method
bool execLocally = false;
Test returnValue = mathUtilsFromClient.ExecuteWithReturnSharedRpc(
    new Test(0, "3 SQL statements walk into a NoSQL bar. Soon, they walk out. They couldn't find a table.", 0), execLocally
);
Console.WriteLine("Joke: " + returnValue.someText);
```
The ``execLocally`` parameter controls if the method should get executed on the local machine or on the remote.

Async methods are also supported:
```cs
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

// Calling the methods
await mathUtilsFromClient.ExecuteSomethingAsyncServerRpc();
var returnValue = await mathUtilsFromClient.ExecuteSomethingAsyncWithReturnServerRpc();
Console.WriteLine("Joke: " + returnValue.someText);
```
The method doesn't need to be async and can only return a task instead.

## Sample projects
More samples can be found in the [Samples project](/Samples).

## License
HyperNetworking is licensed under the MIT License, see [LICENSE.txt](/LICENSE.txt) for more information.
