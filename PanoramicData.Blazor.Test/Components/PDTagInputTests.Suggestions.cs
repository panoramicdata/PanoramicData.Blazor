using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Suggestion list tests for <see cref="PDTagInput"/>.
/// </summary>
public partial class PDTagInputTests
{
	/// <summary>Verifies that focusing offers every unused suggestion, with the first active.</summary>
	[Fact]
	public async Task Focusing_offers_the_unused_suggestions()
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["Red"])
			.Add(x => x.Suggestions, ["Red", "Green", "Blue"]));

		await component.Find(".pd-taginput-input").FocusAsync(new FocusEventArgs());

		Suggestions(component).Should().Equal("Green", "Blue");
		ActiveSuggestion(component).Should().Be("Green");
	}

	/// <summary>Verifies that typing filters the suggestions to those containing the text.</summary>
	[Fact]
	public async Task Typing_filters_the_suggestions()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Green", "Grey", "Blue"]));

		await TypeAsync(component, "gr");

		Suggestions(component).Should().Equal("Green", "Grey");
	}

	/// <summary>Verifies that typing text matching no suggestion hides the list.</summary>
	[Fact]
	public async Task Typing_text_matching_nothing_hides_the_suggestions()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Green"]));

		await TypeAsync(component, "zzz");

		component.FindAll(".pd-taginput-dropdown").Should().BeEmpty();
	}

	/// <summary>Verifies that the arrow keys move the active suggestion, wrapping at both ends, and Enter adds it.</summary>
	[Fact]
	public async Task Arrow_keys_move_the_active_suggestion_and_Enter_adds_it()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Red", "Green", "Blue"]));
		await component.Find(".pd-taginput-input").FocusAsync(new FocusEventArgs());

		await PressAsync(component, "ArrowUp");
		ActiveSuggestion(component).Should().Be("Blue");
		await PressAsync(component, "ArrowDown");
		ActiveSuggestion(component).Should().Be("Red");
		await PressAsync(component, "ArrowDown");
		await PressAsync(component, "Enter");

		Tags(component).Should().Equal("Green");
		Suggestions(component).Should().Equal("Red", "Blue");
	}

	/// <summary>Verifies that Escape hides the suggestions and ArrowDown brings them back.</summary>
	[Fact]
	public async Task Escape_hides_and_ArrowDown_reopens_the_suggestions()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Red"]));
		await component.Find(".pd-taginput-input").FocusAsync(new FocusEventArgs());

		await PressAsync(component, "Escape");
		component.FindAll(".pd-taginput-dropdown").Should().BeEmpty();

		await PressAsync(component, "ArrowDown");
		Suggestions(component).Should().Equal("Red");
	}

	/// <summary>Verifies that adding the last remaining suggestion closes the list.</summary>
	[Fact]
	public async Task Adding_the_last_suggestion_closes_the_list()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Red"]));
		await component.Find(".pd-taginput-input").FocusAsync(new FocusEventArgs());

		await PressAsync(component, "Enter");

		Tags(component).Should().Equal("Red");
		component.FindAll(".pd-taginput-dropdown").Should().BeEmpty();
	}

	/// <summary>Verifies that hovering a suggestion makes it active, and clicking it adds it and refocuses.</summary>
	[Fact]
	public async Task Hovering_and_clicking_a_suggestion_adds_it()
	{
		var component = RenderInput(p => p.Add(x => x.Suggestions, ["Red", "Green"]));
		await component.Find(".pd-taginput-input").FocusAsync(new FocusEventArgs());

		await component.FindAll(".pd-taginput-dropdown li")[1].MouseOverAsync(new MouseEventArgs());
		ActiveSuggestion(component).Should().Be("Green");
		await component.FindAll(".pd-taginput-dropdown li")[1].ClickAsync(new MouseEventArgs());

		Tags(component).Should().Equal("Green");
		JSInterop.Invocations.Should().Contain(i => i.Identifier.EndsWith("focus", StringComparison.Ordinal));
	}
}
