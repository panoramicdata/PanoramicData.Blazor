using BlazorMonaco.Languages;

namespace PanoramicData.Blazor.Models.Monaco;

public class MethodCache
{
	private readonly Dictionary<string, MethodDictionary> _languageDict = [];

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

		public bool HideDataTypes { get; set; }

		public Func<Type, string> TypeNameFn { get; set; } = (type) => type.GetFriendlyTypeName();
	}

	public class Method
	{
		public string Description { get; set; } = string.Empty;

		public string MethodName { get; set; } = string.Empty;

		public string Namespace { get; set; } = string.Empty;

		public List<Parameter> Parameters { get; set; } = [];

		public Type? ReturnType { get; set; }

		public object? State { get; set; }

		public string TypeName { get; set; } = string.Empty;

		public string Fullname => $"{Namespace}.{TypeName}.{MethodName}".TrimStart('.');

		public bool IsMatch(string name)
		{
			var shortName = $"{TypeName}.{MethodName}".TrimStart('.');
			return name.Equals(Fullname, StringComparison.OrdinalIgnoreCase)
				|| name.Equals(shortName, StringComparison.OrdinalIgnoreCase)
				|| name.Equals(MethodName, StringComparison.OrdinalIgnoreCase);
		}

		public override string ToString() => ToString(new());

		public string ToString(MethodCacheOptions options)
		{
			var signature = new StringBuilder();
			if (options.HideDataTypes)
			{
			}
			else if (ReturnType is null)
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

			if (Type is not null && !options.HideDataTypes)
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
			_languageDict.Add(language, []);
		}

		if (_languageDict.TryGetValue(language, out MethodDictionary? methodDict))
		{
			// ensure parameters have position / ordinals
			UpdateParameterPositions(method);

			if (!methodDict.ContainsKey(method.Fullname))
			{
				methodDict.Add(method.Fullname, []);
			}

			if (methodDict.TryGetValue(method.Fullname, out List<Method>? value))
			{
				value.Add(method);
			}
		}
	}

	public static void AddMethodParameters(Method method, IEnumerable<Parameter> parameters)
	{
		method.Parameters.AddRange(parameters);
		UpdateParameterPositions(method);
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
			descriptionProvider?.AddDescriptions(method);

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

	public bool Contains(string language) => _languageDict.ContainsKey(language);

	public IEnumerable<Method> FindMethod(string language, string name)
	{
		if (_languageDict.TryGetValue(language, out MethodDictionary? methodDict)
			&& methodDict.TryGetValue(name, out List<Method>? value))
		{
			return value;
		}

		return [];
	}

	public IEnumerable<CompletionItem> GetCompletionItems(string language, string functionName)
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

					// add parameters?
					if (!string.IsNullOrWhiteSpace(functionName) && functionName == kvp.Key)
					{
						if (kvp.Value.Count == 0)
						{
							// Todo: fetch parameters?
						}

						var m = kvp.Value.First();
						foreach (var p in m.Parameters)
						{
							items.Add(new CompletionItem
							{
								LabelAsString = p.Name,
								DocumentationAsString = p.Description,
								Kind = CompletionItemKind.Property,
								InsertText = p.Name
							});
						}
					}
				}
			}
		}

		return items;
	}

	public IEnumerable<SignatureInformation> GetSignatures(string language, string name)
	{
		var signatures = new List<SignatureInformation>();
		if (!string.IsNullOrWhiteSpace(name))
		{
			if (_languageDict.TryGetValue(language, out MethodDictionary? methodDict))
			{
				var methods = methodDict.Values.SelectMany(overloads => overloads.Where(method => method.IsMatch(name))).ToArray();
				foreach (var method in methods)
				{
					signatures.Add(new SignatureInformation
					{
						Label = method.ToString(Options),
						Parameters = method.Parameters.Select(p => new ParameterInformation
						{
							Label = p.ToString(Options),
							Documentation = p.Description
						}).ToArray()
					});
				}
			}
		}

		return signatures;
	}

	public MethodCacheOptions Options { get; private set; } = new();

	private static void UpdateParameterPositions(Method method)
	{
		var allParamsBarFirst = method.Parameters.Where(x => x.Position == 0).OrderBy(x => x.Position).Skip(1).ToList();
		if (allParamsBarFirst.Count > 0)
		{
			var position = allParamsBarFirst.Max(p => p.Position);
			allParamsBarFirst.ForEach(p => p.Position = p.Position == 0 ? ++position : p.Position);
		}
	}

}