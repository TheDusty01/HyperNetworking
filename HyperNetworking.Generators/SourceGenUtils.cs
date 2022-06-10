using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class IsExternalInit { }
}

namespace HyperNetworking.Generators
{
    internal record Parameter(string Name, string FullyQualifiedTypeName, bool HasDefaultValue, object? DefaultValue, bool IsGreedy = false);
    internal record Method(IMethodSymbol MethodSymbol, string Name, string FullyQualifiedReturnTypeName, Parameter[] Parameters, bool IsAsync)
    {
        public bool IsVoid { get => MethodSymbol.ReturnsVoid; }
        public bool IsTask { get => MethodSymbol.ReturnType.OriginalDefinition.ToString() == "System.Threading.Tasks.Task"; }
        public bool IsGenericTask { get => MethodSymbol.ReturnType.OriginalDefinition.ToString() == "System.Threading.Tasks.Task<TResult>"; }
    }

    internal enum RpcType
    {
        Server,
        Client,
        Shared
    }

    internal record RpcTypeName(RpcType Type, string Name)
    {
        public static readonly RpcTypeName Server = new(RpcType.Server, "HyperNetworking.Messaging.ServerRpcAttribute");
        public static readonly RpcTypeName Client = new(RpcType.Client, "HyperNetworking.Messaging.ClientRpcAttribute");
        public static readonly RpcTypeName Shared = new(RpcType.Shared, "HyperNetworking.Messaging.SharedRpcAttribute");

        public static implicit operator string(RpcTypeName rpcType)
        {
            return rpcType.Name;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal static class RpcTypeExt
    {
        internal static string ToStringCached(this RpcType enumValue)
        {
            return enumValue switch
            {
                RpcType.Server => nameof(RpcType.Server),
                RpcType.Client => nameof(RpcType.Client),
                RpcType.Shared => nameof(RpcType.Shared),
                _ => enumValue.ToString(),
            };
        }
    }

    internal class SourceGenUtils
    {
        public static bool IsGenericTask(ITypeSymbol typeSymbol)
        {
            return typeSymbol is INamedTypeSymbol taskType && taskType.OriginalDefinition.ToString() == "System.Threading.Tasks.Task<TResult>";
        }

        public static ITypeSymbol? UnwrapTask(ITypeSymbol typeSymbol)
        {
            // Unwrap Task<T>
            if (typeSymbol is INamedTypeSymbol taskType && IsGenericTask(typeSymbol))
            {
                return taskType.TypeArguments.First();
            }

            return null;
        }

        public static string GetMethodSignature(IMethodSymbol methodSymbol)
        {
            var parameters = string.Join(", ", methodSymbol.Parameters
                .Select(t => t.Type.ToString()));

            // {methodSymbol.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)}.

            return $"{methodSymbol.ReturnType.ContainingNamespace}.{methodSymbol.ReturnType.Name} {methodSymbol.Name}({parameters})";
        }

        public static IEnumerable<INamedTypeSymbol> ScanForServices(SemanticModel semantic)
        {
            var rpcServiceBase = semantic.Compilation.GetTypeByMetadataName("HyperNetworking.Messaging.RpcService");

            if (rpcServiceBase == null)
            {
                yield break;
            }

            var allNodes = semantic.SyntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

            // Loop over all types in that syntax tree
            foreach (var node in allNodes)
            {
                if (semantic.GetDeclaredSymbol(node) is INamedTypeSymbol classSymbol && InheritsFrom(classSymbol, rpcServiceBase))
                {
                    yield return classSymbol;
                }
            }
        }


        public static IEnumerable<Method> ScanForRpcMethods(INamedTypeSymbol classSymbol, string attributeTypeName)
        {
            foreach (var member in classSymbol.GetMembers())
            {
                string methodName = member.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (member is IMethodSymbol methodSymbol &&
                    methodSymbol.DeclaredAccessibility == Accessibility.Public &&
                    !methodSymbol.IsAbstract &&
                    methodSymbol.MethodKind != MethodKind.Constructor)
                {
                    // Skip property methods
                    if (methodSymbol.Name.StartsWith("get_") || methodSymbol.Name.StartsWith("set_"))
                    {
                        continue;
                    }

                    // Make sure to filter method with the specified attribute
                    var attribute = FindAttribute(methodSymbol, a =>
                    {
                        return a.ToString() == attributeTypeName;
                    });
                    if (attribute is null)
                        continue;

                    string name = methodSymbol.Name;
                    ITypeSymbol returnType = methodSymbol.ReturnType;

                    var parameters = methodSymbol.Parameters
                        .Select(t => new Parameter(t.Name, t.Type.ToString(), t.HasExplicitDefaultValue, t.HasExplicitDefaultValue ? t.ExplicitDefaultValue : null))
                        .ToArray();

                    yield return new Method(methodSymbol, name, returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), parameters, methodSymbol.IsAsync);
                }
            }
        }

        public static AttributeData? FindAttribute(ISymbol symbol, Func<INamedTypeSymbol, bool> selectAttribute)
        { 
           return symbol.GetAttributes()
                .LastOrDefault(a => a?.AttributeClass != null && selectAttribute(a.AttributeClass));
        }

        private static bool InheritsFrom(INamedTypeSymbol classDeclaration, INamedTypeSymbol targetBaseType)
        {
            var currentDeclared = classDeclaration;

            while (currentDeclared.BaseType != null)
            {
                var currentBaseType = currentDeclared.BaseType;

                if (currentBaseType.Equals(targetBaseType, SymbolEqualityComparer.Default))
                {
                    return true;
                }

                currentDeclared = currentDeclared.BaseType;
            }

            return false;
        }
    }
}
