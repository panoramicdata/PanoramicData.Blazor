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

	public static string GetPropertyName<TItem>(this Expression<Func<TItem, object>> expr)
	{
		if (expr != null)
		{
			var body = expr.Body.ToString();
			if (expr is MemberExpression)
			{
				return body.Contains('.') ? string.Join(".", body.Split('.').Skip(1)) : body;
			}
			else if (expr is ConditionalExpression ce1 && ce1.IfTrue is MemberExpression tme)
			{
				return tme.ToString().Contains('.') ? string.Join(".", tme.ToString().Split('.').Skip(1)) : tme.ToString();
			}
			else if (expr is ConditionalExpression ce2 && ce2.IfFalse is MemberExpression fme)
			{
				return fme.ToString().Contains('.') ? string.Join(".", fme.ToString().Split('.').Skip(1)) : fme.ToString();
			}
			else
			{
				var idx1 = body.IndexOf("Convert(", StringComparison.Ordinal);
				var idx2 = body.IndexOf(',', StringComparison.Ordinal);
				if (idx1 > -1 && idx2 > idx1)
				{
					body = body[(idx1 + 8)..idx2];
					var path = body.Contains('.') ? string.Join(".", body.Split('.').Skip(1)) : body;
					return path;
				}
			}
		}
		return string.Empty;
	}
}
