using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tag entry and removal tests for <see cref="PDTagInput"/>: committing typed text, rejection rules and removing tags.
/// </summary>
public partial class PDTagInputTests
{
	/// <summary>Verifies that Enter commits the trimmed typed text as a tag and reports it.</summary>
	[Fact]
	public async Task Enter_commits_the_typed_text()
	{
		var component = RenderInput();

		await TypeAsync(component, "  gamma ");
		await PressAsync(component, "Enter");

		Tags(component).Should().Equal("gamma");
		_emitted.Should().ContainSingle().Which.Should().Equal("gamma");
		_added.Should().Equal("gamma");
		component.Find(".pd-taginput-input").GetAttribute("value").Should().BeEmpty();
	}

	/// <summary>Verifies that Enter with only whitespace typed adds nothing.</summary>
	[Fact]
	public async Task Enter_with_blank_text_adds_nothing()
	{
		var component = RenderInput();

		await TypeAsync(component, "   ");
		await PressAsync(component, "Enter");

		Tags(component).Should().BeEmpty();
		_emitted.Should().BeEmpty();
	}

	/// <summary>Verifies that commas commit each complete part and leave the remainder being typed.</summary>
	[Fact]
	public async Task Commas_commit_each_complete_part()
	{
		var component = RenderInput();

		await TypeAsync(component, "one, two,,thr");

		Tags(component).Should().Equal("one", "two");
		component.Find(".pd-taginput-input").GetAttribute("value").Should().Be("thr");
	}

	/// <summary>Verifies that a duplicate is rejected regardless of case, marking the input invalid until typing resumes.</summary>
	[Fact]
	public async Task A_duplicate_is_rejected_until_typing_resumes()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["Alpha"]));

		await TypeAsync(component, "alpha");
		await PressAsync(component, "Enter");

		_rejected.Should().ContainSingle().Which.Reason.Should().Be(TagRejectionReason.Duplicate);
		_rejected[0].Tag.Should().Be("alpha");
		component.Find(".pd-taginput-control").ClassList.Should().Contain("pd-taginput--invalid");

		await TypeAsync(component, "alph");
		component.Find(".pd-taginput-control").ClassList.Should().NotContain("pd-taginput--invalid");
	}

	/// <summary>Verifies that a case-sensitive input treats differently cased text as a different tag.</summary>
	[Fact]
	public async Task A_case_sensitive_input_allows_different_casing()
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["Alpha"])
			.Add(x => x.CaseSensitive, true));

		await TypeAsync(component, "alpha");
		await PressAsync(component, "Enter");

		Tags(component).Should().Equal("Alpha", "alpha");
		_rejected.Should().BeEmpty();
	}

	/// <summary>Verifies that the tag count and tag length limits reject what exceeds them.</summary>
	[Theory]
	[InlineData(1, 0, "second", TagRejectionReason.MaxTagsReached)]
	[InlineData(0, 3, "long", TagRejectionReason.TooLong)]
	public async Task Limits_reject_what_exceeds_them(int maxTags, int maxLength, string text, TagRejectionReason reason)
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["one"])
			.Add(x => x.MaxTags, maxTags)
			.Add(x => x.MaxTagLength, maxLength));

		await TypeAsync(component, text);
		await PressAsync(component, "Enter");

		_rejected.Should().ContainSingle().Which.Reason.Should().Be(reason);
		Tags(component).Should().Equal("one");
	}

	/// <summary>
	/// Verifies that without free text only suggestions are accepted, and a match takes the suggestion's casing.
	/// </summary>
	[Fact]
	public async Task Without_free_text_only_suggestions_are_accepted()
	{
		var component = RenderInput(p => p
			.Add(x => x.AllowFreeText, false)
			.Add(x => x.Suggestions, ["Red", "Green"]));

		await TypeAsync(component, "blue");
		await PressAsync(component, "Escape");
		await PressAsync(component, "Enter");
		await TypeAsync(component, "green");
		await PressAsync(component, "Escape");
		await PressAsync(component, "Enter");

		_rejected.Should().ContainSingle().Which.Reason.Should().Be(TagRejectionReason.NotInSuggestions);
		Tags(component).Should().Equal("Green");
	}

	/// <summary>Verifies that the remove button removes its tag and reports the removal.</summary>
	[Fact]
	public async Task The_remove_button_removes_its_tag()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["alpha", "beta"]));

		await component.FindAll(".pd-taginput-remove")[0].ClickAsync(new MouseEventArgs());

		Tags(component).Should().Equal("beta");
		_removed.Should().Equal("alpha");
		_emitted.Should().ContainSingle().Which.Should().Equal("beta");
	}

	/// <summary>Verifies that Backspace in an empty input removes the last tag, but not while text is typed.</summary>
	[Fact]
	public async Task Backspace_removes_the_last_tag_only_when_nothing_is_typed()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["alpha", "beta"]));

		await TypeAsync(component, "x");
		await PressAsync(component, "Backspace");
		Tags(component).Should().Equal("alpha", "beta");

		await TypeAsync(component, string.Empty);
		await PressAsync(component, "Backspace");
		Tags(component).Should().Equal("alpha");
		_removed.Should().Equal("beta");
	}
}
