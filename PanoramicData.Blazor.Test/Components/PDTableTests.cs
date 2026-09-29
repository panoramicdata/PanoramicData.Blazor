using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Exceptions;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests the rendering, data loading, sorting, paging, selection, keyboard, editing, filtering, drag and drop
/// and state persistence behaviour of <see cref="PDTable{TItem}"/>.
/// </summary>
/// <remarks>
/// Refresh keeping the selection is covered separately by <see cref="PDTableRefreshSelectionTests"/>.
/// </remarks>
public partial class PDTableTests : BunitContext
{
	private readonly ItemProvider _provider = new();
	private readonly BunitJSModuleInterop _common;

	/// <summary>Sets up the rendering context and the table's common JavaScript module.</summary>
	public PDTableTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		_common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
	}

	#region Rendering

	/// <summary>Rows render one cell per column, keyed by the key field, with the column titles as headers.</summary>
	[Fact]
	public void Rows_and_headers_render_from_the_data_provider()
	{
		var table = RenderTable();

		var rows = Rows(table);
		rows.Select(r => r.Id).Should().Equal("1", "2", "3");
		rows[1].QuerySelectorAll("td").Select(td => td.TextContent.Trim()).Should().Equal("Beta", "20");
		table.FindAll("thead th").Select(th => th.Id).Should().Equal("col-name", "col-score");
		table.Find("div.pdtable").ClassList.Should().NotContain("disabled");
	}

	/// <summary>An empty result shows the no-data message across all columns.</summary>
	[Fact]
	public void An_empty_result_shows_the_no_data_message()
	{
		_provider.Items.Clear();
		var table = RenderTable(p => p.Add(x => x.NoDataMessage, "Nothing here"), waitForRows: false);

		table.WaitForAssertion(() => table.Find("tbody td").TextContent.Trim().Should().Be("Nothing here"));
		table.Find("tbody td").GetAttribute("colspan").Should().Be("2");
	}

	/// <summary>A disabled table carries the disabled class and ignores row clicks.</summary>
	[Fact]
	public void A_disabled_table_is_marked_and_ignores_clicks()
	{
		var clicks = 0;
		var table = RenderTable(p => p
			.Add(x => x.IsEnabled, false)
			.Add(x => x.CssClass, "mine")
			.Add(x => x.Click, (Item _) => clicks++));

		table.Find("div.pdtable").ClassList.Should().Contain("disabled").And.Contain("mine");
		Rows(table)[0].Click();
		clicks.Should().Be(0);
	}

	/// <summary>Clicking and double-clicking a row raise Click and DoubleClick with the row's item.</summary>
	[Fact]
	public void Row_click_and_double_click_raise_callbacks()
	{
		var clicked = new List<string>();
		var table = RenderTable(p => p
			.Add(x => x.Click, (Item item) => clicked.Add($"click {item.Name}"))
			.Add(x => x.DoubleClick, (Item item) => clicked.Add($"double {item.Name}")));

		Rows(table)[0].Click();
		Rows(table)[1].DoubleClick();

		clicked.Should().Equal("click Alpha", "double Beta");
	}

	/// <summary>Row classes combine the selection, disabled state and the RowClass function.</summary>
	[Fact]
	public void Row_classes_reflect_selection_enabled_state_and_row_class()
	{
		var table = RenderTable(p => p
			.Add(x => x.SelectionMode, TableSelectionMode.Single)
			.Add(x => x.RowIsEnabled, item => item.Id != 3)
			.Add(x => x.RowClass, item => item.Id == 2 ? "special" : string.Empty));

		Rows(table)[0].MouseUp(new MouseEventArgs { Button = 0 });

		var rows = Rows(table);
		rows[0].ClassList.Should().Contain("selected");
		rows[1].ClassList.Should().Contain("special").And.NotContain("selected");
		rows[2].ClassList.Should().Contain("disabled");
	}

	/// <summary>
	/// Cells are unselectable by default; a column may opt back in, carries its td and th classes, a header
	/// template, help text and a copy button.
	/// </summary>
	[Fact]
	public void Column_presentation_options_are_rendered()
	{
		var table = RenderTable(columns:
		[
			new Col("col-name", x => x.Name)
			{
				Extra =
				{
					[nameof(PDColumn<Item>.TdClass)] = "td-x",
					[nameof(PDColumn<Item>.ThClass)] = "th-x",
					[nameof(PDColumn<Item>.HelpText)] = "The name",
					[nameof(PDColumn<Item>.ShowCopyButton)] = (Func<Item?, bool>)(_ => true)
				}
			},
			new Col("col-score", x => x.Score)
			{
				Extra =
				{
					[nameof(PDColumn<Item>.UserSelectable)] = true,
					[nameof(PDColumn<Item>.HeaderTemplate)] = (RenderFragment)(b => b.AddMarkupContent(0, "<em>Points</em>")),
					[nameof(PDColumn<Item>.Template)] = (RenderFragment<Item>)(item => b => b.AddContent(0, $"#{item.Score}"))
				}
			}
		]);

		var nameHeader = table.Find("th#col-name");
		nameHeader.ClassList.Should().Contain("th-x").And.Contain("noselect");
		nameHeader.QuerySelector("span.text-nowrap")!.GetAttribute("title").Should().Be("The name");
		table.Find("th#col-score").ClassList.Should().NotContain("noselect");
		table.Find("th#col-score em").TextContent.Should().Be("Points");
		var cells = Rows(table)[0].QuerySelectorAll("td");
		cells[0].ClassList.Should().Contain("td-x").And.Contain("noselect");
		cells[1].TextContent.Trim().Should().Be("#10");
		table.FindComponents<PDClipboard>().Should().HaveCount(3);
	}

	/// <summary>A scrolling table wraps its content in a container with the maximum height and a sticky header.</summary>
	[Fact]
	public void MaxHeight_renders_a_scrolling_container_with_sticky_header()
	{
		var table = RenderTable(p => p
			.Add(x => x.MaxHeight, "300px")
			.Add(x => x.StickyHeader, true)
			.Add(x => x.StickyPager, true)
			.Add(x => x.ShowPager, true)
			.Add(x => x.PagerPosition, PagerPositions.Bottom)
			.Add(x => x.PageCriteria, new PageCriteria(1, 10))
			.Add(x => x.PagerCssClass, "pager-x"));

		var container = table.Find(".pdtable-container");
		container.GetAttribute("style").Should().Contain("max-height: 300px");
		table.Find("thead").ClassList.Should().Contain("pdtable-sticky-header");
		table.FindAll("nav.pdpager").Should().ContainSingle();
		container.QuerySelector("nav.pdpager.pager-x").Should().NotBeNull();
		container.QuerySelector(".pdtable")!.Id.Should().Be(table.Instance.Id);
	}

	/// <summary>A scrolling table without a sticky pager puts the pager after the container.</summary>
	[Fact]
	public void MaxHeight_without_sticky_pager_places_the_pager_outside()
	{
		var table = RenderTable(p => p
			.Add(x => x.MaxHeight, "300px")
			.Add(x => x.ShowPager, true)
			.Add(x => x.PagerPosition, PagerPositions.Bottom)
			.Add(x => x.PagerCssClass, "pager-x")
			.Add(x => x.PageCriteria, new PageCriteria(1, 10)));

		table.FindAll("nav.pdpager").Should().ContainSingle();
		table.Find(".pdtable-container").QuerySelector("nav.pdpager").Should().BeNull();
		table.Find("nav.pdpager").ClassList.Should().Contain("pager-x");
		table.Find("thead").ClassList.Should().NotContain("pdtable-sticky-header");
	}

	/// <summary>The pager is rendered above, below or on both sides according to the pager position.</summary>
	[Theory]
	[InlineData(PagerPositions.Top, 1)]
	[InlineData(PagerPositions.Bottom, 1)]
	[InlineData(PagerPositions.Both, 2)]
	public void Pager_position_controls_how_many_pagers_render(PagerPositions position, int expected)
	{
		var table = RenderTable(p => p
			.Add(x => x.ShowPager, true)
			.Add(x => x.PagerPosition, position)
			.Add(x => x.PageCriteria, new PageCriteria(1, 10)));

		table.FindComponents<PDPager>().Should().HaveCount(expected);
	}

	/// <summary>ColumnsConfig selects and orders the columns shown, ignoring ids it does not know.</summary>
	[Fact]
	public void ColumnsConfig_selects_and_orders_columns()
	{
		var table = RenderTable(p => p.Add(x => x.ColumnsConfig,
		[
			new PDColumnConfig { Id = "col-score" },
			new PDColumnConfig { Id = "col-missing" },
			new PDColumnConfig { Id = "col-name" }
		]));

		table.FindAll("thead th").Select(th => th.Id).Should().Equal("col-score", "col-name");
		Rows(table)[0].QuerySelectorAll("td").Select(td => td.TextContent.Trim()).Should().Equal("10", "Alpha");
	}

	/// <summary>A column registered with a default id is given one derived from its title.</summary>
	[Fact]
	public void A_default_column_id_is_replaced_with_one_from_the_title()
	{
		var table = RenderTable(columns: [new Col(null, x => x.Name) { Extra = { [nameof(PDColumn<Item>.Title)] = "Full Name!" } }]);

		table.Instance.Columns.Single().Id.Should().Be("col-fullname");
	}

	/// <summary>Selection without a key field is refused with a clear exception.</summary>
	[Fact]
	public void Selection_without_a_key_field_is_refused()
	{
		var act = () => Render<PDTable<Item>>(parameters => parameters
			.Add(p => p.DataProvider, _provider)
			.Add(p => p.SelectionMode, TableSelectionMode.Single));

		act.Should().Throw<PDTableException>().WithMessage("*KeyField*");
	}

	#endregion

	private IRenderedComponent<PDTable<Item>> RenderEditable(
		Action<ComponentParameterCollectionBuilder<PDTable<Item>>>? configure = null,
		Col[]? columns = null)
		=> RenderTable(p =>
		{
			p.Add(x => x.AllowEdit, true).Add(x => x.SelectionMode, TableSelectionMode.Single);
			configure?.Invoke(p);
		}, columns: columns);

	private IRenderedComponent<PDTable<Item>> RenderInDragContext(PDDragContext context, Action<ComponentParameterCollectionBuilder<PDTable<Item>>> configure)
		=> RenderTable(p =>
		{
			p.AddCascadingValue(context);
			configure(p);
		});
	private static Col FilterColumn()
		=> new("col-name", x => x.Name) { Extra = { [nameof(PDColumn<Item>.Filterable)] = true, [nameof(PDColumn<Item>.FilterKey)] = "name" } };

	private static void MouseUp(IRenderedComponent<PDTable<Item>> table, int row, bool ctrl = false, bool shift = false, long button = 0)
		=> Rows(table)[row].MouseUp(new MouseEventArgs { Button = button, CtrlKey = ctrl, ShiftKey = shift });

	private static void Key(IRenderedComponent<PDTable<Item>> table, string code, bool ctrl = false)
		=> table.Find("div.pdtable").KeyDown(new KeyboardEventArgs { Code = code, CtrlKey = ctrl });

	private IRenderedComponent<PDTable<Item>> RenderTable(
		Action<ComponentParameterCollectionBuilder<PDTable<Item>>>? configure = null,
		bool waitForRows = true,
		Col[]? columns = null)
	{
		var table = Render<PDTable<Item>>(parameters =>
		{
			parameters
				.Add(p => p.DataProvider, _provider)
				.Add(p => p.KeyField, item => item.Id)
				.Add(p => p.ChildContent, Columns(columns ?? DefaultColumns()));
			configure?.Invoke(parameters);
		});

		if (waitForRows)
		{
			table.WaitForAssertion(() => table.FindAll("tbody tr.pdtablerow").Should().NotBeEmpty());
		}

		return table;
	}

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

	private static List<AngleSharp.Dom.IElement> Rows(IRenderedComponent<PDTable<Item>> table)
		=> [.. table.FindAll("tbody tr.pdtablerow")];

	private static List<string> Names(IRenderedComponent<PDTable<Item>> table)
		=> [.. table.Instance.ItemsToDisplay.Select(i => i.Name)];

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
