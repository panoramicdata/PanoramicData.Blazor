using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDTagInput"/> adds, rejects and removes tags from keyboard, comma, blur and
/// suggestion input, reports each change, and keeps its suggestion list in step.
/// </summary>
public partial class PDTagInputTests : BunitContext
{
	/// <summary>
	/// How long to wait for a render that another thread or a timer brings about. Generous because a busy
	/// machine (the whole suite under coverage) can hold the renderer's dispatcher well past bUnit's default.
	/// </summary>
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	private readonly List<List<string>> _emitted = [];
	private readonly List<string> _added = [];
	private readonly List<string> _removed = [];
	private readonly List<TagRejectedEventArgs> _rejected = [];

	/// <summary>Sets up the rendering context.</summary>
	public PDTagInputTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Verifies that the supplied values render as removable tags, with no placeholder.</summary>
	[Fact]
	public void Supplied_values_render_as_removable_tags()
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["alpha", "beta"])
			.Add(x => x.Id, "tags")
			.Add(x => x.CssClass, "wide")
			.AddUnmatched("data-test", "yes"));

		Tags(component).Should().Equal("alpha", "beta");
		component.FindAll(".pd-taginput-remove").Select(b => b.GetAttribute("aria-label"))
			.Should().Equal("Remove alpha", "Remove beta");
		component.Find(".pd-taginput-input").GetAttribute("placeholder").Should().BeEmpty();
		var root = component.Find(".pd-taginput");
		root.Id.Should().Be("tags");
		root.ClassList.Should().Contain("wide");
		root.GetAttribute("data-test").Should().Be("yes");
	}

	/// <summary>Verifies that an empty input shows its placeholder.</summary>
	[Fact]
	public void An_empty_input_shows_the_placeholder()
	{
		var component = RenderInput(p => p.Add(x => x.Placeholder, "Type a tag"));

		component.Find(".pd-taginput-input").GetAttribute("placeholder").Should().Be("Type a tag");
	}

	/// <summary>Verifies that a disabled or read-only input shows tags but offers no way to change them.</summary>
	[Theory]
	[InlineData(false, false, "pd-taginput--disabled")]
	[InlineData(true, true, "pd-taginput--readonly")]
	public void Disabled_and_read_only_inputs_cannot_be_changed(bool isEnabled, bool isReadOnly, string stateClass)
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["alpha"])
			.Add(x => x.IsEnabled, isEnabled)
			.Add(x => x.IsReadOnly, isReadOnly));

		Tags(component).Should().Equal("alpha");
		component.FindAll(".pd-taginput-remove").Should().BeEmpty();
		component.FindAll(".pd-taginput-input").Should().BeEmpty();
		component.Find(".pd-taginput-control").ClassList.Should().Contain(stateClass);
	}

	/// <summary>Verifies that a tag template renders each tag's content.</summary>
	[Fact]
	public void A_tag_template_renders_each_tag()
	{
		var component = RenderInput(p => p
			.Add(x => x.Values, ["alpha"])
			.Add(x => x.TagTemplate, tag => (RenderFragment)(b => b.AddMarkupContent(0, $"<b>#{tag}</b>"))));

		component.Find(".pd-taginput-tag b").TextContent.Should().Be("#alpha");
		component.FindAll(".pd-taginput-tag-label").Should().BeEmpty();
	}

	private IRenderedComponent<PDTagInput> RenderInput(Action<ComponentParameterCollectionBuilder<PDTagInput>>? configure = null)
		=> Render<PDTagInput>(parameters =>
		{
			parameters
				.Add(p => p.ValuesChanged, (List<string> values) => _emitted.Add(values))
				.Add(p => p.TagAdded, (string tag) => _added.Add(tag))
				.Add(p => p.TagRemoved, (string tag) => _removed.Add(tag))
				.Add(p => p.TagRejected, (TagRejectedEventArgs args) => _rejected.Add(args));
			configure?.Invoke(parameters);
		});

	private static Task TypeAsync(IRenderedComponent<PDTagInput> component, string text)
		=> component.Find(".pd-taginput-input").InputAsync(new ChangeEventArgs { Value = text });

	private static Task PressAsync(IRenderedComponent<PDTagInput> component, string key)
		=> component.Find(".pd-taginput-input").KeyDownAsync(new KeyboardEventArgs { Key = key });

	private static List<string> Tags(IRenderedComponent<PDTagInput> component)
		=> [.. component.FindAll(".pd-taginput-tag").Select(t => t.GetAttribute("title")!)];

	private static List<string> Suggestions(IRenderedComponent<PDTagInput> component)
		=> [.. component.FindAll(".pd-taginput-dropdown li").Select(li => li.TextContent.Trim())];

	private static string ActiveSuggestion(IRenderedComponent<PDTagInput> component)
		=> component.Find(".pd-taginput-dropdown li.active").TextContent.Trim();
}
