using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFilter"/> offers the operators its data type and options allow, loads and
/// selects candidate values when opened, builds the filter from the operator, typed values and clicked
/// values, and applies or clears it through <see cref="PDFilter.FilterChanged"/>.
/// </summary>
public class PDFilterTests : BunitContext
{
	private readonly List<Filter> _changes = [];
	private readonly List<Filter> _fetchRequests = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDFilterTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>
	/// Verifies the operators offered for text: the text operators, with no ordering or null operators.
	/// </summary>
	[Fact]
	public void Text_offers_text_operators_only()
	{
		var component = RenderFilter(FilterDataTypes.Text);

		OptionValues(component).Should().Equal(
			"Equals", "DoesNotEqual", "In", "NotIn",
			"StartsWith", "EndsWith", "Contains", "DoesNotContain", "IsEmpty", "IsNotEmpty");
	}

	/// <summary>
	/// Verifies the operators offered for a nullable number: ordering, range and null operators.
	/// </summary>
	[Fact]
	public void A_nullable_number_offers_ordering_range_and_null_operators()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, nullable: true);

		OptionValues(component).Should().Equal(
			"Equals", "DoesNotEqual", "In", "NotIn",
			"GreaterThan", "GreaterThanOrEqual", "LessThan", "LessThanOrEqual", "Range",
			"IsNull", "IsNotNull");
	}

	/// <summary>
	/// Verifies that operators disabled in the options are not offered.
	/// </summary>
	[Fact]
	public void Disabled_operators_are_not_offered()
	{
		var options = new FilterOptions
		{
			AllowEquals = false,
			AllowDoesNotEqual = false,
			AllowIn = false,
			AllowNotIn = false,
			AllowGreaterThan = false,
			AllowGreaterThanOrEqual = false,
			AllowLessThan = false,
			AllowLessThanOrEqual = false,
			AllowRange = false,
			AllowIsNull = false
		};

		var component = RenderFilter(FilterDataTypes.Date, nullable: true, options: options);

		OptionValues(component).Should().Equal("IsNotNull");
	}

	/// <summary>
	/// Verifies that the text operators can each be withheld by the options.
	/// </summary>
	[Fact]
	public void Disabled_text_operators_are_not_offered()
	{
		var options = new FilterOptions
		{
			AllowStartsWith = false,
			AllowEndsWith = false,
			AllowContains = false,
			AllowDoesNotContain = false,
			AllowIsEmpty = false,
			AllowIsNotEmpty = false
		};

		var component = RenderFilter(FilterDataTypes.Text, options: options);

		OptionValues(component).Should().Equal("Equals", "DoesNotEqual", "In", "NotIn");
	}

	/// <summary>
	/// Verifies that the filtered class reflects whether the current filter does anything.
	/// </summary>
	[Theory]
	[InlineData(FilterTypes.Equals, "", false)]
	[InlineData(FilterTypes.Equals, "x", true)]
	[InlineData(FilterTypes.IsNull, "", true)]
	[InlineData(FilterTypes.IsNotNull, "", true)]
	[InlineData(FilterTypes.IsEmpty, "", true)]
	[InlineData(FilterTypes.IsNotEmpty, "", true)]
	public void The_filtered_class_shows_whether_a_filter_applies(FilterTypes type, string value, bool filtered)
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(type, "name", value));

		component.Find(".pd-filter").ClassList.Contains("filtered").Should().Be(filtered);
	}

	/// <summary>
	/// Verifies that opening fetches values containing the values filter for the filter's key, lists them,
	/// and ticks those already in the filter.
	/// </summary>
	[Fact]
	public async Task Opening_fetches_lists_and_ticks_the_current_values()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.In, "name", "Beta|\"Gamma Ray\""));

		await OpenAsync(component);

		_fetchRequests.Should().ContainSingle();
		_fetchRequests[0].Key.Should().Be("name");
		_fetchRequests[0].FilterType.Should().Be(FilterTypes.Contains);
		ValueLabels(component).Select(l => l.TextContent.Trim()).Should().Equal("Alpha", "Beta", "Gamma Ray");
		TickedValues(component).Should().Equal("Beta", "Gamma Ray");
	}

	/// <summary>
	/// Verifies that a display function changes the listed text of each value.
	/// </summary>
	[Fact]
	public async Task A_display_function_changes_the_listed_text()
	{
		var component = RenderFilter(FilterDataTypes.Text, display: v => v.ToUpperInvariant());

		await OpenAsync(component);

		ValueLabels(component).Select(l => l.TextContent.Trim()).Should().Equal("ALPHA", "BETA", "GAMMA RAY");
	}

	/// <summary>
	/// Verifies that the values list is not shown, nor fetched, when ShowValues is off.
	/// </summary>
	[Fact]
	public async Task Values_are_not_fetched_or_shown_when_turned_off()
	{
		var component = RenderFilter(FilterDataTypes.Text, showValues: false);

		await OpenAsync(component);

		_fetchRequests.Should().BeEmpty();
		component.FindAll(".filter-values").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that typing a value and pressing Filter applies an Equals filter with that value.
	/// </summary>
	[Fact]
	public async Task Typing_a_value_and_filtering_applies_it()
	{
		var component = RenderFilter(FilterDataTypes.Text);
		await OpenAsync(component);

		ValueTextBoxes(component)[0].Change("typed");
		ClickFilter(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.Equals);
		filter.Value.Should().Be("typed");
	}

	/// <summary>
	/// Verifies that clicking one value selects it for Equals, and a second switches to an In list,
	/// quoting values that contain whitespace.
	/// </summary>
	[Fact]
	public async Task Clicking_values_builds_an_equals_then_an_in_filter()
	{
		var component = RenderFilter(FilterDataTypes.Text);
		await OpenAsync(component);

		ValueLabels(component)[0].MouseDown();
		TickedValues(component).Should().Equal("Alpha");
		ValueLabels(component)[2].MouseDown();
		ClickFilter(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.In);
		filter.Value.Should().Be("Alpha|\"Gamma Ray\"");
	}

	/// <summary>
	/// Verifies that two values under Does not equal become a Not In list.
	/// </summary>
	[Fact]
	public async Task Two_values_under_does_not_equal_become_not_in()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.DoesNotEqual, "name", string.Empty));
		await OpenAsync(component);

		ValueLabels(component)[0].MouseDown();
		ValueLabels(component)[1].MouseDown();
		ClickFilter(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.NotIn);
		filter.Value.Should().Be("Alpha|Beta");
	}

	/// <summary>
	/// Verifies that clicking a selected value again deselects it and clears the value.
	/// </summary>
	[Fact]
	public async Task Clicking_a_selected_value_again_deselects_it()
	{
		var component = RenderFilter(FilterDataTypes.Text);
		await OpenAsync(component);

		ValueLabels(component)[1].MouseDown();
		ValueLabels(component)[1].MouseDown();
		ClickFilter(component);

		TickedValues(component).Should().BeEmpty();
		_changes.Should().ContainSingle().Which.Value.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that when In is not allowed a click replaces the selection instead of adding to it.
	/// </summary>
	[Fact]
	public async Task Without_in_a_click_replaces_the_selection()
	{
		var component = RenderFilter(FilterDataTypes.Text, options: FilterOptions.SingleValue());
		await OpenAsync(component);

		ValueLabels(component)[0].MouseDown();
		ValueLabels(component)[1].MouseDown();
		ClickFilter(component);

		TickedValues(component).Should().Equal("Beta");
		_changes.Should().ContainSingle().Which.Value.Should().Be("Beta");
	}

	/// <summary>
	/// Verifies that an ordering operator keeps a single selection, replacing it on each click.
	/// </summary>
	[Fact]
	public async Task An_ordering_operator_keeps_a_single_value()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, filter: new Filter(FilterTypes.GreaterThan, "size", "Alpha"));
		await OpenAsync(component);
		TickedValues(component).Should().Equal("Alpha");

		ValueLabels(component)[1].MouseDown();
		ClickFilter(component);

		TickedValues(component).Should().Equal("Beta");
		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.GreaterThan);
		filter.Value.Should().Be("Beta");
	}

	/// <summary>
	/// Verifies that two clicked values under Range become its sorted lower and upper bounds.
	/// </summary>
	[Fact]
	public async Task Two_values_under_range_become_sorted_bounds()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, filter: new Filter(FilterTypes.Range, "size", string.Empty));
		await OpenAsync(component);
		component.FindAll(".filter-body input").Should().HaveCount(2, "a range has from and to boxes");

		ValueLabels(component)[2].MouseDown();
		ValueLabels(component)[0].MouseDown();
		ClickFilter(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.Range);
		filter.Value.Should().Be("Alpha");
		filter.Value2.Should().Be("Gamma Ray");
	}

	/// <summary>
	/// Verifies that opening an existing range ticks both bounds, and typed bounds are applied.
	/// </summary>
	[Fact]
	public async Task An_existing_range_is_ticked_and_typed_bounds_are_applied()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, filter: new Filter(FilterTypes.Range, "size", "Alpha", "Beta"));
		await OpenAsync(component);
		TickedValues(component).Should().Equal("Alpha", "Beta");

		ValueTextBoxes(component)[0].Change("1");
		ValueTextBoxes(component)[1].Change("9");
		ClickFilter(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.Value.Should().Be("1");
		filter.Value2.Should().Be("9");
	}

	/// <summary>
	/// Verifies that operators needing no value hide the value box.
	/// </summary>
	[Fact]
	public async Task Operators_needing_no_value_hide_the_value_box()
	{
		var component = RenderFilter(FilterDataTypes.Text, showValues: false, filter: new Filter(FilterTypes.IsEmpty, "name", string.Empty));

		await OpenAsync(component);

		component.FindAll(".filter-body input").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that typing an In list ticks the values it names.
	/// </summary>
	[Fact]
	public async Task Typing_an_in_list_ticks_the_named_values()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.In, "name", string.Empty));
		await OpenAsync(component);

		ValueTextBoxes(component)[0].Change("Alpha|\"Gamma Ray\"");

		TickedValues(component).Should().Equal("Alpha", "Gamma Ray");
	}

	/// <summary>
	/// Verifies that changing the operator to In joins the selected values into a list.
	/// </summary>
	[Fact]
	public async Task Changing_the_operator_to_in_joins_the_selection()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.In, "name", "Alpha|Beta"));
		await OpenAsync(component);

		component.Find("select").Change(nameof(FilterTypes.NotIn));
		ClickFilter(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.NotIn);
		filter.Value.Should().Be("Alpha|Beta");
	}

	/// <summary>
	/// Verifies that changing to a single-value operator keeps only the first selected value.
	/// </summary>
	[Fact]
	public async Task Changing_to_a_single_value_operator_keeps_the_first_value()
	{
		var component = RenderFilter(FilterDataTypes.Text, filter: new Filter(FilterTypes.In, "name", "Beta|Alpha"));
		await OpenAsync(component);

		component.Find("select").Change(nameof(FilterTypes.Equals));
		ClickFilter(component);

		TickedValues(component).Should().Equal("Beta");
		_changes.Should().ContainSingle().Which.Value.Should().Be("Beta");
	}

	/// <summary>
	/// Verifies that changing to Range sorts the selection into lower and upper bounds.
	/// </summary>
	[Fact]
	public async Task Changing_to_range_sorts_the_selection_into_bounds()
	{
		var component = RenderFilter(FilterDataTypes.Numeric, filter: new Filter(FilterTypes.In, "size", "Gamma Ray|Alpha"));
		await OpenAsync(component);

		component.Find("select").Change(nameof(FilterTypes.Range));
		ClickFilter(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.Value.Should().Be("Alpha");
		filter.Value2.Should().Be("Gamma Ray");
	}

	/// <summary>
	/// Verifies that Select all ticks every value into an In list, and a second click clears them.
	/// </summary>
	[Fact]
	public async Task Select_all_ticks_every_value_then_clears_them()
	{
		var component = RenderFilter(FilterDataTypes.Text, showSelectAll: true);
		await OpenAsync(component);
		component.Find(".values-select-all-count").TextContent.Should().Be("0 / 3");

		component.Find(".values-select-all").Click();
		component.Find(".values-select-all-count").TextContent.Should().Be("3 / 3");
		ClickFilter(component);
		_changes[^1].FilterType.Should().Be(FilterTypes.In);
		_changes[^1].Value.Should().Be("Alpha|Beta|\"Gamma Ray\"");

		await OpenAsync(component);
		component.Find(".values-select-all").Click();
		TickedValues(component).Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that Select all shows a partial icon when only some values are ticked.
	/// </summary>
	[Fact]
	public async Task Select_all_shows_a_partial_icon_for_some_values()
	{
		var component = RenderFilter(FilterDataTypes.Text, showSelectAll: true, filter: new Filter(FilterTypes.In, "name", "Beta"));

		await OpenAsync(component);

		component.Find(".values-select-all i").ClassList.Should().Contain("fa-minus-square");
	}

	/// <summary>
	/// Verifies that typing in the values filter fetches the values again with that text.
	/// </summary>
	[Fact]
	public async Task The_values_filter_refetches_with_its_text()
	{
		var component = RenderFilter(FilterDataTypes.Text);
		await OpenAsync(component);
		var valuesBox = component.FindComponents<PDTextBox>().Single(t => t.Instance.DebounceWait > 0);

		await component.InvokeAsync(() => valuesBox.Instance.OnDebouncedInput("gam"));

		_fetchRequests.Should().HaveCount(2);
		_fetchRequests[^1].Value.Should().Be("gam");
	}

	/// <summary>
	/// Verifies that Clear Filter resets the filter to an empty Equals and reports it.
	/// </summary>
	[Fact]
	public async Task Clear_resets_and_reports_the_filter()
	{
		var filter = new Filter(FilterTypes.Contains, "name", "abc");
		var component = RenderFilter(FilterDataTypes.Text, filter: filter);
		await OpenAsync(component);

		component.Find(".filter-toolbar .btn-secondary").Click();

		_changes.Should().ContainSingle().Which.Should().BeSameAs(filter);
		filter.FilterType.Should().Be(FilterTypes.Equals);
		filter.Value.Should().BeEmpty();
		component.Find(".pd-filter").ClassList.Should().NotContain("filtered");
	}

	/// <summary>
	/// Verifies that Enter in the drop down focuses and clicks the Filter button through JavaScript,
	/// while other keys do nothing.
	/// </summary>
	[Fact]
	public async Task Enter_focuses_and_clicks_the_filter_button()
	{
		var common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		var component = RenderFilter(FilterDataTypes.Text);
		var dropDown = component.FindComponent<PDDropDown>().Instance;
		var buttonId = component.Find(".filter-toolbar .btn-primary").Id;

		await component.InvokeAsync(() => dropDown.OnKeyPressed(65));
		await component.InvokeAsync(() => dropDown.OnKeyPressed(13));

		common.Invocations["focus"].Should().ContainSingle().Which.Arguments.Should().Equal(buttonId);
		common.Invocations["click"].Should().ContainSingle().Which.Arguments.Should().Equal(buttonId);
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

	private static void ClickFilter(IRenderedComponent<PDFilter> component)
		=> component.Find(".filter-toolbar .btn-primary").Click();

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
