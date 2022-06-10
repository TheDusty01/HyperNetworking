using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace HyperNetworking.Generators
{
    // REF:
    // https://github.com/surgicalcoder/ApiClientGenerator/blob/master/GoLive.Generator.ApiClientGenerator/Scanner.cs

    [Generator]
    public class RpcGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            if (!Debugger.IsAttached)
            {
                //Debugger.Launch();
            }

        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;

            var services = compilation.SyntaxTrees
                .Select(t => compilation.GetSemanticModel(t))
                .SelectMany(SourceGenUtils.ScanForServices)
                .ToList();

            foreach (var service in services)
            {
                var sharedRpcs = SourceGenUtils.ScanForRpcMethods(service, RpcTypeName.Shared);
                var clientRpcs = SourceGenUtils.ScanForRpcMethods(service, RpcTypeName.Client);
                var serverRpcs = SourceGenUtils.ScanForRpcMethods(service, RpcTypeName.Server);


                // Build up the source code
                CodeWriter sourceBuilder = new CodeWriter($@"// Auto-generated code
using System;
using HyperNetworking.Core;
using HyperNetworking.Messaging;

namespace {service.ContainingNamespace.ToDisplayString()}
{{
    public partial class {service.Name}
    {{", 2);

                // Generate methods for every SharedRpc marked method
                foreach (var rpc in sharedRpcs)
                {
                    GenerateRpcImplementation(sourceBuilder, rpc, RpcType.Shared);    // TODO: IDK
                }

                // Generate methods for every ClientRpc marked method
                foreach (var rpc in clientRpcs)
                {
                    GenerateRpcImplementation(sourceBuilder, rpc, RpcType.Client);
                }

                // Generate methods for every ServerRpc marked method
                foreach (var rpc in serverRpcs)
                {
                    GenerateRpcImplementation(sourceBuilder, rpc, RpcType.Server);
                }

                sourceBuilder.AppendLine().EndCurlyBrackets().EndCurlyBrackets();

                // Add the source code to the compilation
                context.AddSource($"{service.Name}.g.cs", sourceBuilder.ToString());
            }

        }

        #region Helper
        private const string SharedRpcExecParamName = "execLocally";
        private const string ClientIdsParamName = "clientIds";
        private const string RpcObjectName = "Rpc";
        private const string ServerRpcObjectName = "((ServerRpcEventManager)Rpc)";

        private static void GenerateRpcImplementation(CodeWriter sourceBuilder, Method rpc, RpcType rpcType)
        {
            Parameter[] parameters;
            string rpcObjectName;
            if (rpcType == RpcType.Shared)
            {
                parameters = new Parameter[rpc.Parameters.Length + 1];
                rpc.Parameters.CopyTo(parameters, 0);
                parameters[parameters.Length - 1] = new Parameter(SharedRpcExecParamName, typeof(bool).FullName, false, null);
                rpcObjectName = RpcObjectName;
            }
            else if (rpcType == RpcType.Client)
            {
                parameters = new Parameter[rpc.Parameters.Length + 1];
                rpc.Parameters.CopyTo(parameters, 0);
                parameters[parameters.Length - 1] = new Parameter(ClientIdsParamName, typeof(uint[]).FullName, false, null, true);
                rpcObjectName = ServerRpcObjectName;
            }
            else
            {
                parameters = rpc.Parameters;
                rpcObjectName = RpcObjectName;
            }

            using (sourceBuilder.BeginMethod($"{rpc.Name}{rpcType.ToStringCached()}Rpc", rpc.FullyQualifiedReturnTypeName, parameters, AccessModifier.Public, false, rpc.IsAsync))
            {
                // Format parameters to string
                string paramNames = string.Join(", ", rpc.Parameters.Select(x => x.Name));

                // Execute method locally
                string isLocalCondition = rpcType switch
                {
                    RpcType.Server => "IsLocal(true)",
                    RpcType.Client => "IsLocal(false)",
                    RpcType.Shared => SharedRpcExecParamName,
                    _ => throw new NotImplementedException("RpcType is undefined")
                };

                sourceBuilder.AppendLine(@$"if ({isLocalCondition})");
                using (sourceBuilder.BeginScope())
                {
                    sourceBuilder.AppendLine($"Console.WriteLine(\"Local exec: {rpc.Name}Rpc\");"); // TODO: remove

                    if (rpc.IsVoid)
                    {
                        sourceBuilder.AppendLine($"{rpc.Name}({paramNames});");
                        sourceBuilder.AppendLine("return;");
                    }
                    else if (rpc.IsTask)
                    {
                        sourceBuilder.AppendLine($"{(rpc.IsAsync ? "await" : "return")} {rpc.Name}({paramNames});");
                        if (rpc.IsAsync)
                            sourceBuilder.AppendLine("return;");
                    }
                    else if (rpc.IsGenericTask)
                    {
                        ITypeSymbol unwrappedReturnType = SourceGenUtils.UnwrapTask(rpc.MethodSymbol.ReturnType)!;

                        sourceBuilder
                            .AppendLine($"return {(rpc.IsAsync ? "await " : "")}{rpc.Name}({paramNames});");
                    }
                    else
                    {
                        sourceBuilder.AppendLine($"return {rpc.Name}({paramNames});");
                    }
                }
                sourceBuilder.AppendLine();

                sourceBuilder.AppendLine($"Console.WriteLine(\"Remote exec: {rpc.Name}Rpc\");"); // TODO: remove

                // Get event name for remote call
                var signature = SourceGenUtils.GetMethodSignature(rpc.MethodSymbol);
                var typeName = rpc.MethodSymbol.ContainingType.ToString();

                sourceBuilder.AppendLine($"string name = RpcEventManager.GetEventName(\"{typeName}\", \"{signature}\");");

                // Call event
                string callParentheses;
                if (paramNames.Length > 0)
                {
                    callParentheses = $"({(rpcType == RpcType.Client ? $"{ClientIdsParamName}, " : "")}name, {paramNames})";
                }
                else
                {
                    callParentheses = $"({(rpcType == RpcType.Client ? $"{ClientIdsParamName}, " : "")}name)";
                }

                if (rpc.IsVoid)
                {
                    sourceBuilder.AppendLine($"{(rpc.IsAsync ? "await " : "")}{rpcObjectName}.SendRequest{callParentheses};");
                }
                else if (rpc.IsTask)
                {
                    sourceBuilder.AppendLine($"{(rpc.IsAsync ? "await" : "return")} {rpcObjectName}.SendRequest{callParentheses};");
                }
                else if (rpc.IsGenericTask)
                {
                    ITypeSymbol unwrappedReturnType = SourceGenUtils.UnwrapTask(rpc.MethodSymbol.ReturnType)!;

                    sourceBuilder
                        .AppendLine($"return {(rpc.IsAsync ? "await " : "")}{rpcObjectName}.SendRequest<{unwrappedReturnType!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>{callParentheses};");
                }
                else
                {
                    sourceBuilder.AppendLine($"return {rpcObjectName}.SendRequest<{rpc.FullyQualifiedReturnTypeName}>{callParentheses}.Result;");
                }
            }
        }
        #endregion
    }
}
