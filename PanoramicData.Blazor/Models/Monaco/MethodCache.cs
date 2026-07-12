using BlazorMonaco.Languages;

namespace PanoramicData.Blazor.Models.Monaco;

/// <summary>
/// Caches method signatures and parameters by language for use by the Monaco editor completion and signature-help providers.
/// </summary>
public class MethodCache
{
	private readonly Dictionary<string, MethodDictionary> _languageDict = [];

	/// <summary>
	/// Defines a provider that enriches <see cref="Method"/> instances with human-readable descriptions.
	/// </summary>
	public interface IDescriptionProvider
	{
		/// <summary>
		/// Adds description text to the given method and its parameters.
		/// </summary>
		/// <param name="method">The method whose description should be populated.</param>
		void AddDescriptions(Method method);
	}

	internal sealed class MethodDictionary : Dictionary<string, List<Method>>
	{
	}

	/// <summary>
	/// Configuration options that control how method signatures are formatted in completion and signature-help UI.
	/// </summary>
	public class MethodCacheOptions
	{
		/// <summary>Gets or sets a value indicating whether the declaring type name is prepended to the method name in signatures.</summary>
		public bool IncludeMethodTypeName { get; set; }

		/// <summary>Gets or sets a value indicating whether parameter and return types are omitted from displayed signatures.</summary>
		public bool HideDataTypes { get; set; }

		/// <summary>Gets or sets a function that converts a CLR type to its display name. Defaults to <see cref="ReflectionExtensions.GetFriendlyTypeName"/>.</summary>
		public Func<Type, string> TypeNameFn { get; set; } = (type) => type.GetFriendlyTypeName();
	}

	/// <summary>
	/// Represents a single overload of a method in the method cache.
	/// </summary>
	public class Method
	{
		/// <summary>Gets or sets a human-readable description of the method.</summary>
		public string Description { get; set; } = string.Empty;

		/// <summary>Gets or sets the method name.</summary>
		public string MethodName { get; set; } = string.Empty;

		/// <summary>Gets or sets the namespace of the declaring type.</summary>
		public string Namespace { get; set; } = string.Empty;

		/// <summary>Gets or sets the list of parameters for this overload.</summary>
		public List<Parameter> Parameters { get; set; } = [];

		/// <summary>Gets or sets the CLR return type of the method, or <c>null</c> for void methods.</summary>
		public Type? ReturnType { get; set; }

		/// <summary>Gets or sets arbitrary caller-supplied state associated with this method entry.</summary>
		public object? State { get; set; }

		/// <summary>Gets or sets the simple name of the declaring type.</summary>
		public string TypeName { get; set; } = string.Empty;

		/// <summary>Gets the fully qualified method name in the form <c>Namespace.TypeName.MethodName</c>.</summary>
		public string Fullname => $"{Namespace}.{TypeName}.{MethodName}".TrimStart('.');

		/// <summary>
		/// Returns whether the supplied name matches this method by full name, short name, or bare method name.
		/// </summary>
		/// <param name="name">The name to compare against.</param>
		/// <returns><c>true</c> if the name matches any of the three forms.</returns>
		public bool IsMatch(string name)
		{
			var shortName = $"{TypeName}.{MethodName}".TrimStart('.');
			return name.Equals(Fullname, StringComparison.OrdinalIgnoreCase)
				|| name.Equals(shortName, StringComparison.OrdinalIgnoreCase)
				|| name.Equals(MethodName, StringComparison.OrdinalIgnoreCase);
		}

		/// <inheritdoc />
		public override string ToString() => ToString(new());

		/// <summary>
		/// Returns a formatted method signature string using the given options.
		/// </summary>
		/// <param name="options">Formatting options controlling type names and prefixes.</param>
		/// <returns>The formatted method signature string.</returns>
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

	/// <summary>
	/// Represents a single parameter of a cached method overload.
	/// </summary>
	public class Parameter
	{
		/// <summary>Gets or sets a human-readable description of the parameter.</summary>
		public string Description { get; set; } = string.Empty;

		/// <summary>Gets or sets the parameter name.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets the zero-based position of this parameter in the method signature.</summary>
		public int Position { get; set; }

		/// <summary>Gets or sets a value indicating whether this parameter is optional.</summary>
		public bool IsOptional { get; set; }

		/// <summary>Gets or sets a value indicating whether this parameter accepts a variable argument list (<c>params</c>).</summary>
		public bool IsParams { get; set; }

		/// <summary>Gets or sets a value indicating whether this parameter has a generic type.</summary>
		public bool IsGeneric { get; set; }

		/// <summary>Gets or sets the CLR type of this parameter.</summary>
		public Type? Type { get; set; }

		/// <inheritdoc />
		public override string ToString() => ToString(new());

		/// <summary>
		/// Returns a formatted parameter string using the given options.
		/// </summary>
		/// <param name="options">Formatting options.</param>
		/// <returns>The formatted parameter string.</returns>
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

	/// <summary>
	/// Adds a single method overload to the cache under the given language identifier.
	/// </summary>
	/// <param name="language">The language identifier (e.g. <c>"ncalc"</c>).</param>
	/// <param name="method">The method to add.</param>
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

	/// <summary>
	/// Appends a collection of parameters to an existing method and updates their position ordinals.
	/// </summary>
	/// <param name="method">The method to add parameters to.</param>
	/// <param name="parameters">The parameters to append.</param>
	public static void AddMethodParameters(Method method, IEnumerable<Parameter> parameters)
	{
		method.Parameters.AddRange(parameters);
		UpdateParameterPositions(method);
	}

	/// <summary>
	/// Reflects over the methods of the given type and adds them to the cache under the specified language, optionally enriching descriptions via a provider.
	/// </summary>
	/// <param name="language">The language identifier.</param>
	/// <param name="type">The CLR type whose methods are to be cached.</param>
	/// <param name="flags">Optional binding flags to filter methods; defaults to all public instance methods.</param>
	/// <param name="descriptionProvider">Optional provider that adds descriptions to the discovered methods.</param>
	/// <returns>The number of methods added.</returns>
	public int AddTypeMethods(string language, Type type, BindingFlags? flags = null, IDescriptionProvider? descriptionProvider = null)
	{
		var count = 0;

		// filter methods?
		var methodInfos = flags.HasValue ? type.GetMethods(flags.Value) : type.GetMethods();

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

	/// <summary>
	/// Adds all public static methods from the given type to the cache, optionally enriching them with descriptions.
	/// </summary>
	/// <param name="language">The language identifier.</param>
	/// <param name="type">The CLR type whose public static methods are to be cached.</param>
	/// <param name="descriptionProvider">Optional provider that adds descriptions to the discovered methods.</param>
	/// <returns>The number of methods added.</returns>
	public int AddPublicStaticTypeMethods(string language, Type type, IDescriptionProvider? descriptionProvider = null)
		=> AddTypeMethods(language, type, BindingFlags.Public | BindingFlags.Static, descriptionProvider);

	/// <summary>Removes all cached entries for all languages.</summary>
	public void Clear()
	{
		_languageDict.Clear();
	}

	/// <summary>
	/// Returns whether any methods are cached for the given language identifier.
	/// </summary>
	/// <param name="language">The language identifier to check.</param>
	/// <returns><c>true</c> when the language has at least one cached method.</returns>
	public bool Contains(string language) => _languageDict.ContainsKey(language);

	/// <summary>
	/// Finds all overloads matching the given method name within the specified language.
	/// </summary>
	/// <param name="language">The language identifier.</param>
	/// <param name="name">The method name or full qualified name to find.</param>
	/// <returns>A sequence of matching <see cref="Method"/> overloads.</returns>
	public IEnumerable<Method> FindMethod(string language, string name)
	{
		if (_languageDict.TryGetValue(language, out MethodDictionary? methodDict)
			&& methodDict.TryGetValue(name, out List<Method>? value))
		{
			return value;
		}

		return [];
	}

	/// <summary>
	/// Returns Monaco completion items for all methods in the given language that match the optional function name filter.
	/// </summary>
	/// <param name="language">The language identifier.</param>
	/// <param name="functionName">When non-empty, also emits parameter items for the matching overload.</param>
	/// <returns>A list of completion items.</returns>
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

	/// <summary>
	/// Returns all signature-help information for overloads of the given method name in the specified language.
	/// </summary>
	/// <param name="language">The language identifier.</param>
	/// <param name="name">The method name to find signatures for.</param>
	/// <returns>A list of <see cref="SignatureInformation"/> objects, one per overload.</returns>
	public IEnumerable<SignatureInformation> GetSignatures(string language, string name)
	{
		var signatures = new List<SignatureInformation>();
		if (!string.IsNullOrWhiteSpace(name) && _languageDict.TryGetValue(language, out MethodDictionary? methodDict))
		{
			var methods = methodDict.Values.SelectMany(overloads => overloads.Where(method => method.IsMatch(name))).ToArray();
			foreach (var method in methods)
			{
				signatures.Add(new SignatureInformation
				{
					Label = method.ToString(Options),
					Parameters = [.. method.Parameters.Select(p => new ParameterInformation
						{
							Label = p.ToString(Options),
							Documentation = p.Description
						})]
				});
			}
		}

		return signatures;
	}

	/// <summary>Gets or sets the formatting options used when converting methods and parameters to display strings.</summary>
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