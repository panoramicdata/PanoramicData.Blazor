namespace PanoramicData.Blazor.Extensions;

/// <summary>
/// Extension methods for LINQ expression trees.
/// </summary>
public static class ExpressionExtensions
{
	/// <summary>
	/// Enumerates all <see cref="System.Linq.Expressions.MemberExpression"/> nodes in a chain of member access expressions, yielding each in order from outermost to innermost.
	/// </summary>
	/// <param name="expr">The expression to traverse; may be <c>null</c>.</param>
	/// <returns>A sequence of <see cref="System.Linq.Expressions.MemberExpression"/> nodes, or an empty sequence when <paramref name="expr"/> is not a member expression.</returns>
	public static IEnumerable<MemberExpression> MemberClauses(this Expression? expr)
	{
		if (expr is not MemberExpression mexpr)
		{
			yield break;
		}

		foreach (var item in MemberClauses(mexpr.Expression))
		{
			yield return item;
		}

		yield return mexpr;
	}

	/// <summary>
	/// Extracts a dot-separated property path string from a member-access lambda expression.
	/// Handles direct member access, conditional expressions, and implicit <c>Convert</c> wrappers.
	/// </summary>
	/// <typeparam name="TItem">The source type the expression operates on.</typeparam>
	/// <param name="expr">The lambda expression whose body describes the property path.</param>
	/// <returns>A dot-separated property path (e.g. <c>"Address.City"</c>), or an empty string when the path cannot be determined.</returns>
	public static string GetPropertyName<TItem>(this Expression<Func<TItem, object>> expr)
	{
		if (expr is null)
		{
			return string.Empty;
		}

		return expr.Body switch
		{
			MemberExpression member => StripParameter(member.ToString()),
			ConditionalExpression { IfTrue: MemberExpression trueMember } => StripParameter(trueMember.ToString()),
			ConditionalExpression { IfFalse: MemberExpression falseMember } => StripParameter(falseMember.ToString()),
			_ => GetConvertedPropertyName(expr.Body.ToString())
		};
	}

	private static string GetConvertedPropertyName(string body)
	{
		var idx1 = body.IndexOf("Convert(", StringComparison.Ordinal);
		var idx2 = body.IndexOf(',', StringComparison.Ordinal);
		return idx1 > -1 && idx2 > idx1
			? StripParameter(body[(idx1 + 8)..idx2])
			: string.Empty;
	}

	private static string StripParameter(string path)
		=> path.Contains('.') ? string.Join(".", path.Split('.').Skip(1)) : path;
}
