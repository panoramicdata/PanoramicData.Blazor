using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using PanoramicData.Blazor.Interfaces;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Column definitions and the state store shared by the <see cref="PDTable{TItem}"/> tests.
/// </summary>
public partial class PDTableTests
{
	private static Col FilterColumn()
		=> new("col-name", x => x.Name) { Extra = { [nameof(PDColumn<Item>.Filterable)] = true, [nameof(PDColumn<Item>.FilterKey)] = "name" } };

	private static Col[] DefaultColumns() => [new Col("col-name", x => x.Name), new Col("col-score", x => x.Score)];

	private static RenderFragment Columns(Col[] definitions) => builder =>
	{
		foreach (var column in definitions)
		{
			AddColumn(builder, column);
		}
	};

	private static void AddColumn(RenderTreeBuilder builder, Col column)
	{
		builder.OpenComponent<PDColumn<Item>>(0);
		builder.SetKey(column);
		if (column.Id is not null)
		{
			builder.AddComponentParameter(1, nameof(PDColumn<Item>.Id), column.Id);
		}

		if (column.Field is not null)
		{
			builder.AddComponentParameter(2, nameof(PDColumn<Item>.Field), column.Field);
		}

		foreach (var (name, value) in column.Extra)
		{
			builder.AddComponentParameter(3, name, value);
		}

		builder.CloseComponent();
	}

	/// <summary>A column to render: its id, field and any further parameters.</summary>
	/// <param name="Id">The column id, or null to let the column choose.</param>
	/// <param name="Field">The field the column shows.</param>
	private sealed record Col(string? Id, Expression<Func<Item, object>>? Field)
	{
		public Dictionary<string, object?> Extra { get; } = [];
	}

	/// <summary>An in-memory state store.</summary>
	private sealed class StateStore : IAsyncStateManager
	{
		public Dictionary<string, object> Saved { get; } = [];

		public bool Initialized { get; private set; }

		public Task InitializeAsync()
		{
			Initialized = true;
			return Task.CompletedTask;
		}

		public Task<T?> LoadStateAsync<T>(string key)
			=> Task.FromResult(Saved.TryGetValue(key, out var value) ? (T?)value : default);

		public Task RemoveStateAsync(string key)
		{
			Saved.Remove(key);
			return Task.CompletedTask;
		}

		public Task SaveStateAsync(string key, object state)
		{
			Saved[key] = state;
			return Task.CompletedTask;
		}
	}
}
