﻿using BlazorMonaco.Languages;

namespace PanoramicData.Blazor.Models.Monaco;

public class MethodCache
{
	private readonly Dictionary<string, MethodDictionary> _languageDict = new();

	public interface IDescriptionProvider
	{
		void AddDescriptions(Method method);
	}

	internal class MethodDictionary : Dictionary<string, List<Method>>
	{
	}

	public class MethodCacheOptions
	{
		public bool IncludeMethodTypeName { get; set; }

		public Func<Type, string> TypeNameFn { get; set; } = (type) => type.GetFriendlyTypeName();
	}

	public class Method
	{
		public string Description { get; set; } = string.Empty;

		public string MethodName { get; set; } = string.Empty;

		public string Namespace { get; set; } = string.Empty;

		public List<Parameter> Parameters { get; set; } = new();

		public Type? ReturnType { get; set; }

		public string TypeName { get; set; } = string.Empty;

		public string Fullname => $"{Namespace}.{TypeName}.{MethodName}";

		public override string ToString() => ToString(new());

		public string ToString(MethodCacheOptions options)
		{
			var signature = new StringBuilder();
			if (ReturnType is null)
			{
				signature.Append("void ");
			}
			else
			{
				signature.Append(options.TypeNameFn(ReturnType)).Append(' ');
			}
			if (options.IncludeMethodTypeName)
			{
				signature.Append(TypeName).Append('.');
			}
			signature.Append(MethodName).Append('(');
			foreach (var parameter in Parameters)
			{
				if (parameter.Position > 0)
				{
					signature.Append(", ");
				}
				signature.Append(parameter.ToString(options));
			}
			signature.Append(')');
			return signature.ToString();
		}
	}

	public class Parameter
	{
		public string Description { get; set; } = string.Empty;

		public string Name { get; set; } = string.Empty;

		public int Position { get; set; }

		public bool IsOptional { get; set; }

		public bool IsParams { get; set; }

		public bool IsGeneric { get; set; }

		public Type? Type { get; set; }

		public override string ToString() => ToString(new());

		public string ToString(MethodCacheOptions options)
		{
			var signature = new StringBuilder();
			if (IsOptional)
			{
				signature.Append('[');
			}
			if (IsParams)
			{
				signature.Append("params ");
			}
			if (Type is not null)
			{
				signature.Append(options.TypeNameFn(Type)).Append(' ');
			}
			signature.Append(Name);
			if (IsOptional)
			{
				signature.Append(']');
			}
			return signature.ToString();
		}
	}

	public void AddMethod(string language, Method method)
	{
		if (!_languageDict.ContainsKey(language))
		{
			_languageDict.Add(language, new MethodDictionary());
		}
		if (_languageDict.TryGetValue(language, out MethodDictionary? methodDict))
		{
			if (!methodDict.ContainsKey(method.Fullname))
			{
				methodDict.Add(method.Fullname, new List<Method>());
			}
			if (methodDict.TryGetValue(method.Fullname, out List<Method>? value))
			{
				value.Add(method);
			}
		}
	}

	public int AddTypeMethods(string language, Type type, BindingFlags? flags = null, IDescriptionProvider? descriptionProvider = null)
	{
		var count = 0;

		// filter methods?
		var methodInfos = flags.HasValue ? type.GetMethods(flags.Value) : type.GetMethods();

		var typeNameFn = (string name) => name;

		// iterate over each method
		foreach (MethodInfo methodInfo in methodInfos)
		{
			// create method
			var method = new Method
			{
				Namespace = type.Namespace ?? string.Empty,
				TypeName = type.Name,
				MethodName = methodInfo.GetName(),
				Description = methodInfo.GetDescription(),
				ReturnType = methodInfo.ReturnType
			};
			count++;

			// add parameters
			foreach (var parameterInfo in methodInfo.GetParameters())
			{
				var parameter = new Parameter
				{
					Name = parameterInfo.GetName(),
					Description = parameterInfo.GetDescription(),
					Type = parameterInfo.ParameterType,
					IsOptional = parameterInfo.IsOptional,
					IsParams = parameterInfo.IsDefined(typeof(ParamArrayAttribute), false),
					Position = parameterInfo.Position
				};
				method.Parameters.Add(parameter);
			}

			// enhance method signature with descriptions?
			if (descriptionProvider != null)
			{
				descriptionProvider.AddDescriptions(method);
			}

			AddMethod(language, method);
		}

		return count;
	}

	public int AddPublicStaticTypeMethods(string language, Type type, IDescriptionProvider? descriptionProvider = null)
		=> AddTypeMethods(language, type, BindingFlags.Public | BindingFlags.Static, descriptionProvider);

	public void Clear()
	{
		_languageDict.Clear();
	}

	public IEnumerable<CompletionItem> GetCompletionItems(string language)
	{
		var items = new List<CompletionItem>();
		if (_languageDict.TryGetValue(language, out MethodDictionary? methodDict))
		{
			var functions = new HashSet<string>();

			// iterate over each method
			foreach (var kvp in methodDict)
			{
				if (!functions.Contains(kvp.Key) && kvp.Value.FirstOrDefault() is Method method)
				{
					// build signature from first overload
					var documentation = new StringBuilder();
					documentation.Append(method.ToString(Options));
					var signature = documentation.ToString();

					// overloads?
					if (kvp.Value.Count > 1)
					{
						documentation.AppendLine().AppendLine();
						documentation.Append("(+").Append(kvp.Value.Count - 1).Append(" overloads)");
					}

					if (!string.IsNullOrWhiteSpace(method.Description))
					{
						documentation.AppendLine().AppendLine();
						documentation.AppendLine(method.Description);
					}

					items.Add(new CompletionItem
					{
						LabelAsString = method.MethodName,
						DocumentationAsString = documentation.ToString(),
						Kind = CompletionItemKind.Function,
						InsertText = method.MethodName
					});

					functions.Add(kvp.Key);
				}
			}
		}
		return items;
	}

	public IEnumerable<SignatureInformation> GetSignatures(string language, string name)
	{
		var signatures = new List<SignatureInformation>();
		if (_languageDict.TryGetValue(language, out MethodDictionary? methodDict))
		{
			// filter methods
			if (!string.IsNullOrWhiteSpace(name))
			{
				var nameParts = name.Split('.');
				if (nameParts.Length > 0)
				{
					// name maybe in format: [[namespace.]type.]method
					var ns = "";
					var typeName = "";
					if (nameParts.Length > 2)
					{
						ns = string.Join('.', nameParts[..^3]);
					}
					if (nameParts.Length > 1)
					{
						typeName = nameParts[^2];
					}
					var methodName = nameParts[^1];

					// find matching methods
					var methods = string.IsNullOrEmpty(typeName)
						? methodDict.Values.SelectMany(overloads => overloads.Where(method => method.MethodName.Equals(methodName, StringComparison.OrdinalIgnoreCase)))
						: string.IsNullOrEmpty(ns)
							? methodDict.Values.SelectMany(overloads => overloads.Where(method => method.MethodName.Equals(methodName, StringComparison.OrdinalIgnoreCase)
								&& method.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase)))
							: methodDict.Values.SelectMany(overloads => overloads.Where(method => method.MethodName.Equals(methodName, StringComparison.OrdinalIgnoreCase)
								&& method.TypeName.Equals(typeName, StringComparison.OrdinalIgnoreCase)
								&& method.Namespace.Equals(ns, StringComparison.OrdinalIgnoreCase)));

					foreach (var method in methods)
					{
						signatures.Add(new SignatureInformation
						{
							Label = method.ToString(),
							Parameters = method.Parameters.Select(p => new ParameterInformation
							{
								Label = p.ToString(),
								Documentation = p.Description
							}).ToArray()
						});
					}
				}
			}
		}
		return signatures;
	}

	public MethodCacheOptions Options { get; private set; } = new();
}