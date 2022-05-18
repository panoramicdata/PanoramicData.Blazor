using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace PanoramicData.Blazor.Extensions
{
	public static class IQueryableExtensions
	{
		public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, Filter filter, IKeyedCollection<string>? keyProperties = null)
		{
			try
			{
				var property = keyProperties is null
					? filter.Key
					: keyProperties.Get(filter.Key, filter.Key);
				if (!string.IsNullOrWhiteSpace(property))
				{
					var parameters = filter.FilterType switch
					{
						FilterTypes.In => filter.Value.Split(new[] { "|" }, System.StringSplitOptions.RemoveEmptyEntries),
						_ => new object[] { filter.Value }
					};

					//switch (filter.FilterType)
					//{
					//	case FilterTypes.In:
					//		parameters.AddRange(filter.Value.Split(new[] { "|" }, System.StringSplitOptions.RemoveEmptyEntries));
					//		break;

					//	default:
					//		parameters.Add(filter.Value);
					//		break;
					//}
					var predicate = filter.FilterType switch
					{
						FilterTypes.Contains => $"({property}).Contains(@0)",
						FilterTypes.DoesNotContain => $"!({property}).Contains(@0)",
						FilterTypes.DoesNotEqual => $"{property} != @0",
						FilterTypes.EndsWith => $"({property}).EndsWith(@0)",
						FilterTypes.Equals => $"{property} == @0",
						FilterTypes.StartsWith => $"({property}).StartsWith(@0)",
						FilterTypes.In => string.Join(" || ", parameters.Select((x, i) => $"{property} == @{i}").ToArray()),
						_ => ""
					};
					return string.IsNullOrWhiteSpace(predicate) ? query : query.Where(predicate, parameters);
				}
			}
			catch
			{
				// invalid property
			}
			return query;
		}
	}
}
