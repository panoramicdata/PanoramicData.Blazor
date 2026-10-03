using AwesomeAssertions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// The items and the data provider shared by the <see cref="PDTree{TItem}"/> tests.
/// </summary>
public partial class PDTreeTests
{
	/// <summary>An item in the tree under test, optionally a web link.</summary>
	public sealed class Item(string id, string? parentId, string name) : IWebLink
	{
		/// <summary>Gets the key.</summary>
		public string Id { get; } = id;

		/// <summary>Gets the parent key, or null at the top level.</summary>
		public string? ParentId { get; } = parentId;

		/// <summary>Gets the display text.</summary>
		public string Name { get; } = name;

		/// <inheritdoc />
		public string Target { get; set; } = string.Empty;

		/// <inheritdoc />
		public string Url { get; set; } = string.Empty;

		/// <inheritdoc />
		public override string ToString() => $"Item {Id}";
	}

	/// <summary>
	/// Returns every item, or with load on demand only the children of the key in
	/// <see cref="DataRequest{TItem}.SearchText"/>, and records each request.
	/// </summary>
	private sealed class ItemProvider : DataProviderBase<Item>
	{
		private bool _loadOnDemand;

		/// <summary>Answers each request with only the children of the requested key, as load on demand asks.</summary>
		public void UseLoadOnDemand() => _loadOnDemand = true;

		private Exception? _failure;

		/// <summary>Makes every later request throw <paramref name="failure"/>.</summary>
		public void FailWith(Exception failure) => _failure = failure;

		public List<string?> Requests { get; } = [];

		public List<Item> Items { get; } =
		[
			new("a", null, "Alpha"),
			new("a1", "a", "Alpha one"),
			new("a1x", "a1", "Alpha one x"),
			new("b", null, "Bravo"),
			new("b1", "b", "Bravo one")
		];

		public override Task<DataResponse<Item>> GetDataAsync(DataRequest<Item> request, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Requests.Add(request.SearchText);
			if (_failure != null)
			{
				throw _failure;
			}

			var items = _loadOnDemand
				? Items.Where(i => (i.ParentId ?? string.Empty) == (request.SearchText ?? string.Empty)).ToList()
				: [.. Items];
			return Task.FromResult(new DataResponse<Item>(items, items.Count));
		}
	}
}
