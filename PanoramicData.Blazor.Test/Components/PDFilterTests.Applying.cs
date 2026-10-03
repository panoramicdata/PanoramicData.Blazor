using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFilter"/> applies, clears and shows its filter.
/// </summary>
public partial class PDFilterTests
{
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
	/// Verifies that typing a value and pressing Filter applies an Equals filter with that value.
	/// </summary>
	[Fact]
	public async Task Typing_a_value_and_filtering_applies_it()
	{
		var component = RenderFilter(FilterDataTypes.Text);
		await OpenAsync(component);

		await TypeValueAsync(component, 0, "typed");
		await ClickFilterAsync(component);

		var filter = _changes.Should().ContainSingle().Subject;
		filter.FilterType.Should().Be(FilterTypes.Equals);
		filter.Value.Should().Be("typed");
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

		await component.InvokeAsync(() => component.Find(".filter-toolbar .btn-secondary").ClickAsync(new MouseEventArgs()));

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
}
