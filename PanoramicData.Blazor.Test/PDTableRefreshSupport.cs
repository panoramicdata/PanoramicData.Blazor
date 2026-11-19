using Microsoft.AspNetCore.Components.Rendering;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>A row in the tables the refresh tests render.</summary>
/// <param name="Id">Key.</param>
/// <param name="Name">Display name.</param>
internal sealed record RefreshRow(int Id, string Name);

/// <summary>An in-memory provider that honours search text, sort and paging, as a real one does.</summary>
internal sealed class RefreshRowProvider(int rowCount) : DataProviderBase<RefreshRow>
{
	private static readonly RefreshRow[] _defaultRows = [new(1, "Alpha"), new(2, "Beta"), new(3, "Gamma"), new(4, "Delta")];

	public List<RefreshRow> Rows { get; set; } = [.. _defaultRows.Take(rowCount)];

	public override Task<DataResponse<RefreshRow>> GetDataAsync(DataRequest<RefreshRow> request, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		IEnumerable<RefreshRow> rows = Rows;
		if (!string.IsNullOrWhiteSpace(request.SearchText))
		{
			rows = rows.Where(r => r.Name.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase));
		}

		if (request.SortFieldExpression is not null)
		{
			var key = request.SortFieldExpression.Compile();
			rows = request.SortDirection == SortDirection.Descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
		}

		var all = rows.ToList();
		IEnumerable<RefreshRow> page = all;
		if (request.Skip is { } skip)
		{
			page = page.Skip(skip);
		}

		if (request.Take is { } take)
		{
			page = page.Take(take);
		}

		return Task.FromResult(new DataResponse<RefreshRow>([.. page], all.Count));
	}
}

/// <summary>The one column the refresh tests show.</summary>
internal static class RefreshColumns
{
	public static void Name(RenderTreeBuilder builder, bool sortable)
	{
		builder.OpenComponent<PDColumn<RefreshRow>>(0);
		builder.AddAttribute(1, nameof(PDColumn<RefreshRow>.Id), nameof(RefreshRow.Name));
		builder.AddAttribute(2, nameof(PDColumn<RefreshRow>.Field), (System.Linq.Expressions.Expression<Func<RefreshRow, object>>)(row => row.Name));
		builder.AddAttribute(3, nameof(PDColumn<RefreshRow>.Sortable), sortable);
		builder.CloseComponent();
	}
}
