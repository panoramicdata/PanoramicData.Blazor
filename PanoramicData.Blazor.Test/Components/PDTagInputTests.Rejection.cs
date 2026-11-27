using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests for how <see cref="PDTagInput"/> tells the user a tag was refused.
/// </summary>
public partial class PDTagInputTests
{
	/// <summary>Verifies that a refused tag marks the input invalid for assistive technology, with no opt-in.</summary>
	[Fact]
	public async Task A_refused_tag_sets_aria_invalid_on_the_input()
	{
		var component = RenderInput(p => p.Add(x => x.Values, ["Alpha"]));

		await TypeAsync(component, "alpha");
		await PressAsync(component, "Enter");

		component.Find(".pd-taginput-input").GetAttribute("aria-invalid").Should().Be("true");
	}

	/// <summary>Verifies that ShowRejectionMessage says why a duplicate was refused.</summary>
	[Fact]
	public async Task Rejection_message_says_a_duplicate_is_already_added()
	{
		var component = RenderWithMessage(p => p.Add(x => x.Values, ["Alpha"]));

		await TypeAsync(component, "alpha");
		await PressAsync(component, "Enter");

		Message(component).Should().Be("\"alpha\" is already added.");
	}

	/// <summary>Verifies that ShowRejectionMessage says how long a tag may be.</summary>
	[Fact]
	public async Task Rejection_message_says_how_long_a_tag_may_be()
	{
		var component = RenderWithMessage(p => p.Add(x => x.MaxTagLength, 3));

		await TypeAsync(component, "long");
		await PressAsync(component, "Enter");

		Message(component).Should().Be("Tags can be up to 3 characters.");
	}

	/// <summary>Verifies that ShowRejectionMessage says how many tags are allowed.</summary>
	[Fact]
	public async Task Rejection_message_says_how_many_tags_are_allowed()
	{
		var component = RenderWithMessage(p => p
			.Add(x => x.Values, ["one"])
			.Add(x => x.MaxTags, 1));

		await TypeAsync(component, "two");
		await PressAsync(component, "Enter");

		Message(component).Should().Be("The most tags allowed is 1.");
	}

	/// <summary>Verifies that ShowRejectionMessage asks for a suggestion when free text is not allowed.</summary>
	[Fact]
	public async Task Rejection_message_asks_for_a_tag_from_the_list()
	{
		var component = RenderWithMessage(p => p
			.Add(x => x.AllowFreeText, false)
			.Add(x => x.Suggestions, ["Red"]));

		await TypeAsync(component, "blue");
		await PressAsync(component, "Escape");
		await PressAsync(component, "Enter");

		Message(component).Should().Be("Choose a tag from the list.");
	}

	/// <summary>Verifies that ShowRejectionMessage warns while typing past MaxTagLength, before anything is committed.</summary>
	[Fact]
	public async Task Typing_past_MaxTagLength_warns_before_commit()
	{
		var component = RenderWithMessage(p => p.Add(x => x.MaxTagLength, 3));

		await TypeAsync(component, "long");

		Message(component).Should().Be("Tags can be up to 3 characters.");
		component.Find(".pd-taginput-input").GetAttribute("aria-invalid").Should().Be("true");
		Tags(component).Should().BeEmpty();
		_rejected.Should().BeEmpty();
	}

	/// <summary>Verifies that RejectionMessage replaces the default wording.</summary>
	[Fact]
	public async Task RejectionMessage_replaces_the_default_wording()
	{
		var component = RenderWithMessage(p => p
			.Add(x => x.Values, ["Alpha"])
			.Add(x => x.RejectionMessage, e => $"Not again: {e.Tag}"));

		await TypeAsync(component, "alpha");
		await PressAsync(component, "Enter");

		Message(component).Should().Be("Not again: alpha");
	}

	/// <summary>Verifies that without ShowRejectionMessage nothing new is shown, and typing too much is not flagged early.</summary>
	[Fact]
	public async Task Without_ShowRejectionMessage_no_message_is_shown()
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["Alpha"])
			.Add(x => x.MaxTagLength, 3));

		await TypeAsync(component, "long");
		component.Find(".pd-taginput-control").ClassList.Should().NotContain("pd-taginput--invalid");

		await TypeAsync(component, "alpha");
		await PressAsync(component, "Enter");

		component.FindAll(".pd-taginput-message").Should().BeEmpty();
	}

	/// <summary>Verifies that typing again clears the message and the invalid state.</summary>
	[Fact]
	public async Task Typing_again_clears_the_message()
	{
		var component = RenderWithMessage(p => p.Add(x => x.Values, ["Alpha"]));

		await TypeAsync(component, "alpha");
		await PressAsync(component, "Enter");
		await TypeAsync(component, "alph");

		component.FindAll(".pd-taginput-message").Should().BeEmpty();
		component.Find(".pd-taginput-input").HasAttribute("aria-invalid").Should().BeFalse();
	}

	private IRenderedComponent<PDTagInput> RenderWithMessage(Action<ComponentParameterCollectionBuilder<PDTagInput>> configure)
		=> RenderInput(p =>
		{
			p.Add(x => x.ShowRejectionMessage, true);
			configure(p);
		});

	private static string Message(IRenderedComponent<PDTagInput> component)
		=> component.Find(".pd-taginput-message").TextContent.Trim();
}
