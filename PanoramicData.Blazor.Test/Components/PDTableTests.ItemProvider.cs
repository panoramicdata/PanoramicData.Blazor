using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The rows and the in-memory data provider shared by the <see cref="PDTable{TItem}"/> tests.
/// </summary>
public partial class PDTableTests
{
	/// <summary>A row in the table under test.</summary>
	public sealed class Item
	{
		/// <summary>Gets or sets the key.</summary>
		public int Id { get; set; }

		/// <summary>Gets or sets the display name.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets a numeric value.</summary>
		public int Score { get; set; }
	}

	/// <summary>An in-memory provider that honours sort and paging and records what it is asked.</summary>
	private sealed class ItemProvider : DataProviderBase<Item>
	{
		public List<Item> Items { get; } =
		[
			new() { Id = 1, Name = "Alpha", Score = 10 },
			new() { Id = 2, Name = "Beta", Score = 20 },
			new() { Id = 3, Name = "Gamma", Score = 30 }
		];

		public List<DataRequest<Item>> Requests { get; } = [];

		public List<IDictionary<string, object?>> Updates { get; } = [];

		private Exception? _failure;

		private TaskCompletionSource? _gate;

		/// <summary>Makes every later fetch throw <paramref name="failure"/>.</summary>
		public void FailWith(Exception failure) => _failure = failure;

		/// <summary>Makes later fetches wait until the returned gate is released.</summary>
		public TaskCompletionSource HoldFetches() => _gate = new TaskCompletionSource();

		public CancellationToken LastToken { get; private set; }

		public override async Task<DataResponse<Item>> GetDataAsync(DataRequest<Item> request, CancellationToken cancellationToken)
		{
			Requests.Add(request);
			LastToken = cancellationToken;
			if (_gate is { } gate)
			{
				await gate.Task;
			}

			if (_failure is { } failure)
			{
				throw failure;
			}

			IEnumerable<Item> rows = Items;
			if (request.SortFieldExpression is not null)
			{
				var key = request.SortFieldExpression.Compile();
				rows = request.SortDirection == SortDirection.Descending ? rows.OrderByDescending(key) : rows.OrderBy(key);
			}

			var all = rows.ToList();
			var page = all.Skip(request.Skip ?? 0).Take(request.Take ?? all.Count);
			return new DataResponse<Item>([.. page], all.Count);
		}

		public override Task<OperationResponse> UpdateAsync(Item item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Updates.Add(delta);
			return Task.FromResult(new OperationResponse { Success = Items.Contains(item) });
		}
	}
}
