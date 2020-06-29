using System;
using System.Reflection;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Extensions
{
	public static class ReflectionExtensions
	{
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

		public static Type GetMemberUnderlyingType(this MemberInfo member)
			=> member.MemberType switch
			{
				MemberTypes.Field => ((FieldInfo)member).FieldType,
				MemberTypes.Property => ((PropertyInfo)member).PropertyType,
				MemberTypes.Event => ((EventInfo)member).EventHandlerType,
				_ => throw new ArgumentException("MemberInfo must be if type FieldInfo, PropertyInfo or EventInfo", nameof(member)),
			};

		public static Type GetNonNullableType(this Type type)
			=> Nullable.GetUnderlyingType(type) ?? type;
	}
}
