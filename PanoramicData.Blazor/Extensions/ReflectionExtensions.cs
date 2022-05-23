namespace PanoramicData.Blazor.Extensions;

public static class ReflectionExtensions
{
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
		=> member.MemberType switch
		{
			MemberTypes.Field => ((FieldInfo)member).FieldType,
			MemberTypes.Property => ((PropertyInfo)member).PropertyType,
			MemberTypes.Event => ((EventInfo)member).EventHandlerType,
			_ => throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", nameof(member)),
		};

	/// <summary>
	/// Returns the underlying data type for the given non nullable data type or regular data type.
	/// </summary>
	/// <param name="type">Either a non nullable data type, or a regular data type </param>
	/// <returns>If a non nullable data type was given then returns the underlying data type, otherwise the given data type is returned.</returns>
	public static Type GetNonNullableType(this Type type)
		=> Nullable.GetUnderlyingType(type) ?? type;
}
