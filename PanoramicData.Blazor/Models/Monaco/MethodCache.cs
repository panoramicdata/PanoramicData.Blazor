using BlazorMonaco.Languages;

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

	public class Method
	{
		public string Description { get; set; } = string.Empty;

		public string MethodName { get; set; } = string.Empty;

		public string Namespace { get; set; } = string.Empty;

		public List<Parameter> Parameters { get; set; } = new();

		public Type? ReturnType { get; set; }

		public string TypeName { get; set; } = string.Empty;

		public string Fullname => $"{Namespace}.{TypeName}.{MethodName}";

		public override string ToString()
		{
			var parameters = new StringBuilder();
			Parameters.ForEach(p => parameters.Append(p.Position > 0 ? ", " : "").Append(p.ToString()));
			return $"{ReturnType?.Name ?? "void"} {MethodName}({parameters})";
		}
	}

	public class Parameter
	{
		public string Description { get; set; } = string.Empty;

		public string Name { get; set; } = string.Empty;

		public int Position { get; set; }

		public bool IsOptional { get; set; }

		public bool IsParams { get; set; }

		public Type? Type { get; set; }

		public override string ToString()
		{
			return Type is null ? Name : $"{Type.Name} {Name}";
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

	public int AddPublicStaticMethods(string language, Type type, IDescriptionProvider? descriptionProvider = null)
	{
		var count = 0;

		// get all public static methods
		var methodInfos = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

		// iterate over each method
		foreach (MethodInfo methodInfo in methodInfos)
		{
			// create method
			var method = new Method
			{
				Namespace = type.Namespace ?? string.Empty,
				TypeName = type.Name,
				MethodName = methodInfo.Name,
				Description = methodInfo.GetCustomAttributes().OfType<DisplayAttribute>().SingleOrDefault()?.Description ?? string.Empty,
				ReturnType = methodInfo.ReturnType
			};
			count++;

			// add parameters
			foreach (var parameterInfo in methodInfo.GetParameters())
			{
				var parameter = new Parameter
				{
					Name = parameterInfo.Name ?? string.Empty,
					Description = parameterInfo.GetCustomAttributes().OfType<DisplayAttribute>().SingleOrDefault()?.Description ?? string.Empty,
					Type = parameterInfo.ParameterType,
					IsOptional = parameterInfo.IsOptional,
					Position = parameterInfo.Position
				};
				if (string.IsNullOrEmpty(parameter.Description))
				{

				}
				method.Parameters.Add(parameter);
			}

			// enhance method signature with descriptions?
			if (string.IsNullOrEmpty(method.Description) && descriptionProvider != null)
			{
				descriptionProvider.AddDescriptions(method);
			}

			AddMethod(language, method);
		}

		return count;
	}

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
					documentation.Append(method.ReturnType is null ? "void" : method.ReturnType.Name);
					documentation.Append(' ').Append(method.TypeName).Append('.').Append(method.MethodName).Append('(');
					foreach (var parameter in method.Parameters)
					{
						documentation.Append(parameter.Position > 0 ? ", " : "");
						if (parameter.Type != null)
						{
							documentation.Append(parameter.Type.Name).Append(' ');
						}
						documentation.Append(parameter.Name);
					}
					documentation.Append(')');
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
}