using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace HyperNetworking.Generators
{
	enum AccessModifier
	{
		Private = 0,
		Empty = 1,
		Protected = 2,
		Internal = 3,
		Public = 4
    }

	class CodeWriter
	{
		private ScopeTracker scopeTracker;

		public StringBuilder Content { get; } = new();
		public int IndentLevel { get; private set; } = 0;

		private Stack<ScopeTypeData> scopeStack = new Stack<ScopeTypeData>();

		internal CodeWriter()
		{
			scopeTracker = new ScopeTracker(this);
		}

		internal CodeWriter(string initialContent, int nextIndentLevel = 0) : this()
        {
			Content.AppendLine(initialContent);
			IndentLevel = nextIndentLevel;
        }

		#region String concatenation
		public CodeWriter Append(string text)
		{
			Content.Append(text);
			return this;
		}

		public CodeWriter AppendIndent()
		{
			Content.Append(new string('\t', IndentLevel));
			return this;
		}

		public CodeWriter AppendIndent(string text)
		{
			Content.Append(new string('\t', IndentLevel)).Append(text);
			return this;
		}

		public CodeWriter AppendLine(string line)
		{
			Content.Append(new string('\t', IndentLevel)).AppendLine(line);
			return this;
		}

		public CodeWriter AppendLine()
		{
			Content.AppendLine();
			return this;
		}

		public CodeWriter AppendLineComment(string comment) => AppendLine($"// {comment}");
		public CodeWriter AppendComment(string comment)
        {
			Append($"/* {comment} */");
			return this;
        }

		public CodeWriter StartCurlyBrackets()
        {
			AppendLine("{");
			IndentLevel++;
			return this;
        }

		public CodeWriter EndCurlyBrackets()
		{
			IndentLevel--;
			AppendLine("}");
			return this;
		}

		public CodeWriter StartComment()
        {
			Append("/*");
			return this;
        }

		public CodeWriter EndComment()
        {
			Append("*/");
			return this;
        }

		public CodeWriter StartLine()
		{
			Content.Append(new string('\t', IndentLevel));
			return this;
		}

		public CodeWriter EndLine() => AppendLine();
        #endregion

        #region Scopes
		private ScopeTracker BeginScope(ScopeTypeData scopeTypeData)
		{
			scopeStack.Push(scopeTypeData);

			AppendLine("{");
			IndentLevel += 1;
			return scopeTracker;
		}

		private ScopeTracker BeginScope(ScopeTypeData scopeTypeData, string line)
        {
			AppendLine(line);
			return BeginScope(scopeTypeData);
		}


		public ScopeTracker BeginScope()
		{
			return BeginScope(new ScopeTypeData(ScopeType.Any));
		}

		public ScopeTracker BeginScope(string line)
		{
			AppendLine(line);
			return BeginScope();
		}

		public void EndScope()
		{
			IndentLevel -= 1;
			AppendLine("}");

			scopeStack.Pop();
		}
        #endregion

        #region High level api
		public CodeWriter AddUsing(string usingNamespace)
		{
			AppendLine($"using {usingNamespace};");
			return this;
		}

		public CodeWriter AddUsingAlias(string aliasName, string usingClass)
        {
			AppendLine($"using {aliasName} = {usingClass};");
			return this;
		}

		public CodeWriter AddStaticUsing(string usingClass)
		{
			AppendLine($"using static {usingClass};");
			return this;
		}

		public ScopeTracker BeginNamespace(string name)
		{
			return BeginScope(new ScopeTypeData(ScopeType.Namespace, name),
				$"namespace {name}");
		}

		public ScopeTracker BeginClass(string name, AccessModifier accessModifier = AccessModifier.Empty, bool isPartial = false, bool isStatic = false)
		{
			AppendIndent();
			Append(AccessModifierToString(accessModifier));
			if (isStatic)
				Append("static ");
			if (isPartial)
				Append("partial ");

			AppendLine($"class {name}");

			return BeginScope(new ScopeTypeData(ScopeType.Class, name));
		}

		public ScopeTracker BeginMethod(string name, string returnType, Parameter[] parameters, AccessModifier accessModifier = AccessModifier.Empty, bool isStatic = false, bool isAsync = false)
		{
			AppendLine();
			AppendIndent();
			Append(AccessModifierToString(accessModifier));
			if (isStatic)
				Append("static ");
			if (isAsync)
				Append("async ");

			Append($"{returnType} {name}(");
			for (int i = 0; i < parameters.Length; i++)
			{
				bool isLast = i == parameters.Length - 1;
				var param = parameters[i];
				Append($"{(isLast && param.IsGreedy ? "params " : "")}{param.FullyQualifiedTypeName} {param.Name}");

				if (!isLast)
					Append(", ");
			}

			Append(")").AppendLine();

			return BeginScope(new ScopeTypeData(ScopeType.Method, name));
		}

		public ScopeTracker BeginConstructor(AccessModifier accessModifier = AccessModifier.Empty)
        {
			ScopeTypeData data = scopeStack.Peek();
			if (data is null || data.Type != ScopeType.Class)
            {
				throw new InvalidOperationException("The last scope needs to be class, unable to create constructor!");
            }

			AppendIndent();
			Append(AccessModifierToString(accessModifier));

			AppendLine($"{data.Name}()");

			return BeginScope(new ScopeTypeData(ScopeType.Method, data.Name));

		}
		#endregion

		public override string ToString() => Content.ToString();
		public string EscapeString(string text) => text.Replace("\"", "\"\"");

		private static string AccessModifierToString(AccessModifier accessModifier)
        {
			return accessModifier switch
			{
				AccessModifier.Private => "private ",
				AccessModifier.Protected => "protected ",
				AccessModifier.Internal => "internal ",
				AccessModifier.Public => "public ",
				_ => ""
			};
		}

		class ScopeTypeData
        {
			public ScopeType Type { get; }
			public string? Name { get; }

			public ScopeTypeData(ScopeType type)
            {
                Type = type;
            }

            public ScopeTypeData(ScopeType type, string? name) : this(type)
			{
				Name = name;
            }

        }

		enum ScopeType
        {
			Any = 0,
			Namespace = 1,
			Class = 2,
			Method = 3
        }

		public class ScopeTracker : IDisposable
		{
			public CodeWriter Parent { get; }

			public ScopeTracker(CodeWriter parent)
			{
				Parent = parent;
			}

			public void Dispose()
			{
				Parent.EndScope();
			}
		}
	}
}
