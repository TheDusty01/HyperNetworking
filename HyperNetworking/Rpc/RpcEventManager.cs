using HyperNetworking.Messaging;
using HyperNetworking.Messaging.Packets;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HyperNetworking.Core
{
    public abstract class RpcEventManager
    {
        protected readonly IRpcNetworkParticipant networkParticipant;
        //private readonly bool isServer;

        internal RpcEventManager(IRpcNetworkParticipant networkParticipant)
        {
            this.networkParticipant = networkParticipant;
            //isServer = networkParticipant is NetworkServer;
        }

        internal void Setup()
        {
            //localRpcEvents = new Dictionary<string, RpcAction>();
            //remoteRpcEvents = new Dictionary<string, RpcAction>();
            //remoteRpcEvents = new List<string>();
            //pendingRequest = new Dictionary<Guid, PendingRpcRequest>();

            InitializeServices();
            SetupEvents();
        }

        #region Services
        protected Dictionary<Type, RpcService> singletons = new Dictionary<Type, RpcService>();

        private void InitializeServices()
        {
            var domain = AppDomain.CurrentDomain;
            Type[] classes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                //.Where(t => t.BaseType == typeof(RpcService))
                .Where(t => typeof(RpcService).IsAssignableFrom(t) && !t.IsAbstract)
                //.Where(t => t.GetCustomAttributes(typeof(RpcService), false).Length > 0)
                .ToArray();


            lock (singletons)
            {
                //RpcEventManager netRpc = networkParticipant.Rpc;
                //if (isServer)
                //    netRpc = ((RpcServer)networkParticipant).Rpc;
                //else
                //    netRpc = ((RpcClient)networkParticipant).Rpc;

                foreach (Type type in classes)
                {
                    RpcService service;
                    if (!singletons.ContainsKey(type))
                    {
                        service = (RpcService)Activator.CreateInstance(type);
                        singletons.Add(type, service);
                    }
                    else
                    {
                        service = singletons[type];
                    }

                    //if (isServer)
                    //    service.Server = (NetworkServer)networkParticipant;
                    //else
                    //    service.Client = (NetworkClient)networkParticipant;

                    service.Rpc = networkParticipant.Rpc;
                    service.IsServer = networkParticipant is RpcServer;
                }
            }

        }

        public TService AddService<TService>(TService instance) where TService : RpcService
        {
            // Service is null
            if (instance is null)
                throw new ArgumentNullException(nameof(instance));

            // Service already registered
            if (GetService<TService>() is not null)
                throw new ArgumentException("Service is already registered", nameof(instance));

            lock (singletons)
            {
                singletons.Add(typeof(TService), instance);
            }

            return instance;
        }

        public bool RemoveService<TService>() where TService : RpcService
        {
            lock (singletons)
            {
                return singletons.Remove(typeof(TService));
            }
        }

        public TService? GetService<TService>() where TService : RpcService
        {
            if (!singletons.TryGetValue(typeof(TService), out RpcService service))
                return null;

            return (TService)service;
        }
        #endregion

        #region Events
        protected Dictionary<string, RpcAction> localRpcEvents = new Dictionary<string, RpcAction>();
        //protected Dictionary<string, RpcAction> remoteRpcEvents;
        protected List<string> remoteRpcEvents = new List<string>();
        internal Dictionary<Guid, PendingRpcRequest> pendingRequest = new Dictionary<Guid, PendingRpcRequest>();

        protected IEnumerable<MethodInfo> GetRpcMethods<TRpcAttribute>(Type type) where TRpcAttribute : RpcAttribute
        {
            return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<SharedRpcAttribute>() != null || m.GetCustomAttribute<TRpcAttribute>() != null);
        }

        public static string GetEventName(Type type, MethodInfo method)
        {
            //return $"{type.GUID:N}_{method}";

            string genericTypesFormatted = "";
            string formattedReturnType = method.ReturnType.ToString();
            if (method.ReturnType.IsGenericType)
            {
                var genericArgs = method.ReturnType.GetGenericArguments();
                genericTypesFormatted = $"<{string.Join(", ", genericArgs.Select(t => t.FullName))}>";

                formattedReturnType = formattedReturnType.Split($"`{genericArgs.Length}")[0];   // Remove generic type name
            }

            string parameterTypesForammted = string.Join(", ", method.GetParameters().Select(p => p.ParameterType.FullName));

            string signature = $"{formattedReturnType}{genericTypesFormatted} {method.Name}({parameterTypesForammted})";

            return GetEventName(type, signature);
            //return GetEventName(type, method.ToString());
        }

        public static string GetEventName(Type type, string methodSignature)
        {
            return GetEventName(type.FullName, methodSignature);
        }

        public static string GetEventName(string fullTypeName, string methodSignature)
        {
            return $"{fullTypeName}_{methodSignature}";
        }

        protected abstract void SetupEvents();

        internal RpcResult CallEvent(string name, object[] args)
        {
            // TODO: Move errors to exception field of RpcResult
            if (!localRpcEvents.TryGetValue(name, out RpcAction action))
                throw new Exception("Event " + name + " not found");
            
            if (args.Length != action.methodArgs.Length)
                throw new Exception("Arguments of event " + name + " are not equal to the supplied arguments");

            try
            {
                return new RpcResult(action.Call(args));
            }
            catch (TargetInvocationException ex)
            {
                return new RpcResult(ex.InnerException);
            }
        }

        //private RpcAction GetRemoteEvent(string name)
        //{
        //    RpcAction rpcAction;
        //    if (!remoteRpcEvents.TryGetValue(name, out rpcAction))
        //        return null;

        //    return rpcAction;
        //}

        protected bool DoesRemoteEventExist(string name)
        {
            return remoteRpcEvents.Contains(name);
        }

        #region Request/Response handling
        public abstract Task SendRequest(string eventName, params object[] args);

        public abstract Task<TResult> SendRequest<TResult>(string eventName, params object[] args);

        internal abstract void RecieveResponse(NetworkClient client, RpcResponsePacket resp);
        #endregion

        //public static object CallEvent(string name, object[] args)
        //{
        //    if (!events.TryGetValue(name, out RpcAction action))
        //        throw new Exception("Event " + name + " not found");

        //    object res = null;
        //    try
        //    {
        //        ParameterInfo[] methodArgs = action.Method.GetParameters();

        //        //Console.WriteLine("args: " + args.Length);
        //        //Console.WriteLine("method: " + methodArgs.Length);


        //        if (args.Length != methodArgs.Length)
        //            throw new Exception("Arguments of event " + name + " are not equal to the supplied arguments");

        //        for (int i = 0; i < methodArgs.Length; i++)
        //        {
        //            Type neededType = methodArgs[i].ParameterType;
        //            Type actualType = args[i].GetType();

        //            Console.WriteLine("Needed: " + neededType);
        //            Console.WriteLine("Actual: " + actualType);

        //            if (args[i] is JObject jArg)
        //            {
        //                args[i] = jArg.ToObject(neededType);
        //            }
        //            else if (actualType != neededType)
        //            {
        //                args[i] = Convert.ChangeType(args[i], methodArgs[i].ParameterType);
        //            }
        //        }

        //        res = action.Method.Invoke(action.Instance, args);
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }

        //    return res;
        //}

        internal class RpcResult
        {
            public object? ReturnValue { get; }
            public Exception? Exception { get; }

            public RpcResult(object returnValue)
            {
                ReturnValue = returnValue;
            }

            public RpcResult(Exception exception)
            {
                Exception = exception;
            }

            public RpcResult(object? returnValue, Exception? exception)
            {
                ReturnValue = returnValue;
                Exception = exception;
            }

            public T? GetReturnValue<T>()
            {
                return (T?)ReturnValue;
            }
        }

        internal class PendingRpcRequest
        {
            public Action<RpcResult> action;
            public uint[]? targetClientIds;
            public List<uint>? acknowledgedClientIds;

            public PendingRpcRequest(Action<RpcResult> action)
            {
                this.action = action;
            }

            public PendingRpcRequest(uint[] targetClientIds, Action<RpcResult> action)
            {
                this.action = action;
                this.targetClientIds = targetClientIds;
                acknowledgedClientIds = new List<uint>(targetClientIds.Length);
            }

            public bool HasAcknowledged(uint clientId)
            {
                return acknowledgedClientIds!.Contains(clientId);
            }

            public void Acknowledge(uint clientId)
            {
                acknowledgedClientIds!.Add(clientId);
            }

            public bool CanAcknowledge(uint clientId)
            {
                return targetClientIds.Contains(clientId);
            }

            public bool IsAcknowledged()
            {
                if (targetClientIds!.Length != acknowledgedClientIds!.Count)
                    return false;

                Dictionary<uint, int> dict = new Dictionary<uint, int>();

                foreach (uint member in targetClientIds)
                {
                    if (dict.ContainsKey(member) == false)
                        dict[member] = 1;
                    else
                        dict[member]++;
                }

                foreach (uint member in acknowledgedClientIds)
                {
                    if (dict.ContainsKey(member) == false)
                        return false;
                    else
                        dict[member]--;
                }

                foreach (KeyValuePair<uint, int> kvp in dict)
                {
                    if (kvp.Value != 0)
                        return false;
                }

                return true;
            }
        }

        protected class RpcAction
        {
            private readonly object instance;
            private readonly MethodInfo method;
            public ParameterInfo[] methodArgs;

            public RpcAction(object instance, MethodInfo method)
            {
                this.instance = instance;
                this.method = method;
                methodArgs = method.GetParameters();
            }

            public object Call(object[] args)
            {
                //for (int i = 0; i < args.Length; i++)
                //{
                //    Type neededType = methodArgs[i].ParameterType;
                //    Type actualType = args[i].GetType();

                //    //Console.WriteLine("Needed: " + neededType);
                //    //Console.WriteLine("Actual: " + actualType);

                //    // Move somewhere else
                //    Console.WriteLine($"Type: {args[i]}");
                //    if (actualType != neededType)
                //    {
                //        args[i] = Convert.ChangeType(args[i], methodArgs[i].ParameterType);
                //    }
                //}

                return method.Invoke(instance, args);
            }
        }

        #endregion

    }

    public abstract class RpcEventManager<TLocalRpcAttribute, TRemoteRpcAttribute> : RpcEventManager
        where TLocalRpcAttribute : RpcAttribute
        where TRemoteRpcAttribute : RpcAttribute
    {
        internal RpcEventManager(IRpcNetworkParticipant networkParticipant) : base(networkParticipant)
        {
        }

        protected override void SetupEvents()
        {
            foreach ((Type type, object instance) in singletons)
            {
                // Add local methods
                var localMethods = GetRpcMethods<TLocalRpcAttribute>(type)
                    .ToArray();

                foreach (var localMethod in localMethods)
                {
                    string eventName = GetEventName(type, localMethod);

                    if (localRpcEvents.ContainsKey(eventName))
                    {
                        throw new Exception($"Event name does already exist for local method: {type.FullName}_{localMethod}");
                    }

                    Console.WriteLine("Added local event " + eventName + " to events.");
                    localRpcEvents.Add(eventName, new RpcAction(instance, localMethod));
                }

                // Add remote methods
                var remoteMethods = GetRpcMethods<TRemoteRpcAttribute>(type)
                    .ToArray();

                foreach (var remoteMethod in remoteMethods)
                {
                    string eventName = GetEventName(type, remoteMethod);

                    //if (remoteRpcEvents.ContainsKey(eventName))
                    //{
                    //    throw new Exception($"Event name does already exist for method: {type.FullName}_{remoteMethod}");
                    //}

                    //Console.WriteLine("Added remote event " + eventName + " to events.");
                    //localRpcEvents.Add(eventName, new RpcAction(instance, remoteMethod));

                    if (remoteRpcEvents.Contains(eventName))
                    {
                        throw new Exception($"Event name does already exist for remote method: {type.FullName}_{remoteMethod}");
                    }

                    Console.WriteLine("Added remote event " + eventName + " to events.");
                    remoteRpcEvents.Add(eventName);
                }
            }
        }
    }
}
