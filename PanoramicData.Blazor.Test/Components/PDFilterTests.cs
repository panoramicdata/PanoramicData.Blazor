using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFilter"/> offers the operators its data type and options allow, loads and
/// selects candidate values when opened, builds the filter from the operator, typed values and clicked
/// values, and applies or clears it through <see cref="PDFilter.FilterChanged"/>.
/// </summary>
public partial class PDFilterTests : BunitContext
{
	private readonly List<Filter> _changes = [];
	private readonly List<Filter> _fetchRequests = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDFilterTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDFilter> RenderFilter(
		FilterDataTypes dataType,
		bool nullable = false,
		FilterOptions? options = null,
		Filter? filter = null,
		bool showValues = true,
		bool showSelectAll = false,
		Func<string, string>? display = null)
		=> Render<PDFilter>(parameters => parameters
			.Add(p => p.DataType, dataType)
			.Add(p => p.Nullable, nullable)
			.Add(p => p.Options, options ?? new FilterOptions())
			.Add(p => p.Filter, filter ?? new Filter { Key = "name" })
			.Add(p => p.ShowValues, showValues)
			.Add(p => p.ShowSelectAll, showSelectAll)
			.Add(p => p.FilterValueDisplayFunc, display)
			.Add(p => p.FetchValuesAsync, FetchValues)
			.Add(p => p.FilterChanged, f => _changes.Add(f)));

	private Task<string[]> FetchValues(Filter request)
	{
		_fetchRequests.Add(request);
		return Task.FromResult<string[]>(["Alpha", "Beta", "Gamma Ray"]);
	}

	private static Task OpenAsync(IRenderedComponent<PDFilter> component)
		=> component.InvokeAsync(() => component.FindComponent<PDDropDown>().Instance.OnDropDownShown());

	private static Task ClickFilterAsync(IRenderedComponent<PDFilter> component)
		=> component.InvokeAsync(() => component.Find(".filter-toolbar .btn-primary").ClickAsync(new MouseEventArgs()));

	private static Task ClickValueAsync(IRenderedComponent<PDFilter> component, int index)
		=> component.InvokeAsync(() => ValueLabels(component)[index].MouseDownAsync(new MouseEventArgs()));

	private static Task TypeValueAsync(IRenderedComponent<PDFilter> component, int index, string text)
		=> component.InvokeAsync(() => ValueTextBoxes(component)[index].ChangeAsync(new ChangeEventArgs { Value = text }));

	private static List<string> OptionValues(IRenderedComponent<PDFilter> component)
		=> [.. component.FindAll("select option").Select(o => o.GetAttribute("value") ?? string.Empty)];

	private static IReadOnlyList<AngleSharp.Dom.IElement> ValueLabels(IRenderedComponent<PDFilter> component)
		=> component.FindAll(".values-list .pd-label");

	private static IReadOnlyList<AngleSharp.Dom.IElement> ValueTextBoxes(IRenderedComponent<PDFilter> component)
		=> component.FindAll(".filter-body > .mb-1 input");

	private static List<string> TickedValues(IRenderedComponent<PDFilter> component)
		=> [.. ValueLabels(component)
			.Where(l => l.QuerySelector("i")!.ClassList.Contains("fa-check-square"))
			.Select(l => l.TextContent.Trim())];
}
