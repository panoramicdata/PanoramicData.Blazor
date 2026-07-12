namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for reflection types such as <see cref="System.Reflection.MethodInfo"/> and <see cref="System.Reflection.ParameterInfo"/>.
/// </summary>
public static class ReflectionExtensions
{
	/// <summary>
	/// Returns the description for the given method, first checking <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Description"/> then <see cref="System.ComponentModel.DescriptionAttribute.Description"/>.
	/// </summary>
	/// <param name="methodInfo">The method to inspect.</param>
	/// <returns>The description string, or an empty string when no description attribute is found.</returns>
	public static string GetDescription(this MethodInfo methodInfo)
	{
		// look for Display attribute
		var description = methodInfo.GetCustomAttributes().OfType<DisplayAttribute>().SingleOrDefault()?.Description;
		if (string.IsNullOrEmpty(description))
		{
			// look for Description attribute
			description = methodInfo.GetCustomAttributes().OfType<DescriptionAttribute>().SingleOrDefault()?.Description;
		}

		return description ?? string.Empty;
	}

	/// <summary>
	/// Returns the description for the given parameter, first checking <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Description"/> then <see cref="System.ComponentModel.DescriptionAttribute.Description"/>.
	/// </summary>
	/// <param name="parameterInfo">The parameter to inspect.</param>
	/// <returns>The description string, or an empty string when no description attribute is found.</returns>
	public static string GetDescription(this ParameterInfo parameterInfo)
	{
		// look for Display attribute
		var description = parameterInfo.GetCustomAttributes().OfType<DisplayAttribute>().SingleOrDefault()?.Description;
		if (string.IsNullOrEmpty(description))
		{
			// look for Description attribute
			description = parameterInfo.GetCustomAttributes().OfType<DescriptionAttribute>().SingleOrDefault()?.Description;
		}

		return description ?? string.Empty;
	}

	/// <summary>
	/// Returns the display name for the given method, first checking <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Name"/> then <see cref="System.ComponentModel.DisplayNameAttribute.DisplayName"/>. Falls back to the method's declared name.
	/// </summary>
	/// <param name="methodInfo">The method to inspect.</param>
	/// <returns>The display name, or the method's declared name when no display attribute is found.</returns>
	public static string GetName(this MethodInfo methodInfo)
	{
		// look for Display attribute
		var name = methodInfo.GetCustomAttributes().OfType<DisplayAttribute>().SingleOrDefault()?.Name;
		if (string.IsNullOrEmpty(name))
		{
			// look for Description attribute
			name = methodInfo.GetCustomAttributes().OfType<DisplayNameAttribute>().SingleOrDefault()?.DisplayName;
		}

		return name ?? methodInfo.Name;
	}

	/// <summary>
	/// Returns the display name for the given parameter, first checking <see cref="System.ComponentModel.DataAnnotations.DisplayAttribute.Name"/> then <see cref="System.ComponentModel.DisplayNameAttribute.DisplayName"/>. Falls back to the parameter's declared name.
	/// </summary>
	/// <param name="parameterInfo">The parameter to inspect.</param>
	/// <returns>The display name, or the parameter's declared name when no display attribute is found.</returns>
	public static string GetName(this ParameterInfo parameterInfo)
	{
		// look for Display attribute
		var name = parameterInfo.GetCustomAttributes().OfType<DisplayAttribute>().SingleOrDefault()?.Name;
		if (string.IsNullOrEmpty(name))
		{
			// look for Description attribute
			name = parameterInfo.GetCustomAttributes().OfType<DisplayNameAttribute>().SingleOrDefault()?.DisplayName;
		}

		return name ?? parameterInfo.Name ?? string.Empty;
	}

	/// <summary>
	/// Returns the MemberInfo for the given expression.
	/// </summary>
	/// <typeparam name="T">Data type expression is based on.</typeparam>
	/// <param name="expression">A MemberExpression or UnaryExpression instance.</param>
	/// <returns>If found returns the MemberInfo instance, otherwise null.</returns>
	public static MemberInfo? GetPropertyMemberInfo<T>(this Expression<Func<T, object>> expression)
	{
		if (expression == null)
		{
			return null;
		}

		if (expression.Body is MemberExpression body)
		{
			return body?.Member;
		}
		else if (expression.Body is ConditionalExpression ce)
		{
			if (ce.IfTrue is MemberExpression tme)
			{
				return tme.Member;
			}
			else if (ce.IfFalse is MemberExpression fme)
			{
				return fme.Member;
			}

		}
		// TODO - Use pattern matching
		UnaryExpression ubody = (UnaryExpression)expression.Body;
		return (ubody.Operand as MemberExpression)?.Member;
	}

	/// <summary>
	/// Returns the underlying data type of the member.
	/// </summary>
	/// <param name="member">MemberInfo instance describing the member.</param>
	/// <returns>The data type of the underlying Field, Property or Event.</returns>
	public static Type GetMemberUnderlyingType(this MemberInfo member)
	{

		switch (member.MemberType)
		{
			case MemberTypes.Field:
				return ((FieldInfo)member).FieldType;

			case MemberTypes.Property:
				return ((PropertyInfo)member).PropertyType;

			case MemberTypes.Event:
				if (((EventInfo)member).EventHandlerType is null)
				{
					throw new ArgumentException("MemberInfo is EventInfo however EventHandlerType is null", nameof(member));
				}

				return ((EventInfo)member).EventHandlerType!;

			default:
				throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", nameof(member));
		}
	}

	/// <summary>
	/// Returns the underlying data type for the given non nullable data type or regular data type.
	/// </summary>
	/// <param name="type">Either a non nullable data type, or a regular data type </param>
	/// <returns>If a non nullable data type was given then returns the underlying data type, otherwise the given data type is returned.</returns>
	public static Type GetNonNullableType(this Type type)
		=> Nullable.GetUnderlyingType(type) ?? type;

	/// <summary>
	/// Returns a human-readable type name, resolving generic arguments and mapping primitive CLR types to their C# keyword equivalents (e.g. <c>int</c>, <c>string</c>).
	/// </summary>
	/// <param name="type">The type to format.</param>
	/// <returns>A friendly name string such as <c>List&lt;int&gt;</c> or <c>string</c>.</returns>
	public static string GetFriendlyTypeName(this Type type)
	{
		if (type.IsGenericType)
		{
			// get the name of the generic type without the arity suffix (e.g., `List` instead of `List`1`)
			string genericTypeName = type.Name[..type.Name.IndexOf('`')];

			// get the names of the generic type arguments
			string[] genericArguments = Array.ConvertAll(type.GetGenericArguments(), t => t.GetFriendlyTypeName());

			// combine the generic type name with the argument names
			return $"{genericTypeName}<{string.Join(", ", genericArguments)}>";
		}

		return type switch
		{
			Type t when t.IsGenericType => "",
			Type t when t == typeof(int) => "int",
			Type t when t == typeof(short) => "short",
			Type t when t == typeof(long) => "long",
			Type t when t == typeof(uint) => "uint",
			Type t when t == typeof(ushort) => "ushort",
			Type t when t == typeof(ulong) => "ulong",
			Type t when t == typeof(bool) => "bool",
			Type t when t == typeof(string) => "string",
			Type t when t == typeof(decimal) => "decimal",
			Type t when t == typeof(float) => "float",
			Type t when t == typeof(double) => "double",
			Type t when t == typeof(byte) => "byte",
			Type t when t == typeof(sbyte) => "sbyte",
			Type t when t == typeof(object) => "object",
			Type t when t == typeof(object[]) => "object[]",
			_ => type.Name
		};
	}
}
