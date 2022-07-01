namespace PanoramicData.Blazor.Extensions;

public static class ExpressionExtensions
{
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
}
