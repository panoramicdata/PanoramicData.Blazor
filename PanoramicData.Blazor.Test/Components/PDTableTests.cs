using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

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

	private static List<AngleSharp.Dom.IElement> Rows(IRenderedComponent<PDTable<Item>> table)
		=> [.. table.FindAll("tbody tr.pdtablerow")];

	private static List<string> Names(IRenderedComponent<PDTable<Item>> table)
		=> [.. table.Instance.ItemsToDisplay.Select(i => i.Name)];
}
