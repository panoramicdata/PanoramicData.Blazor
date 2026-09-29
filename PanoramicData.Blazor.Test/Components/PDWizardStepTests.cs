using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDWizardStep"/> registers with its wizard and shows its content only while it is
/// the active step.
/// </summary>
public class PDWizardStepTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDWizardStepTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Verifies that only the first step's content is shown when the wizard opens.</summary>
	[Fact]
	public void Only_the_active_step_renders_its_content()
	{
		var component = Render<PDWizard>(parameters => parameters
			.Add(p => p.ChildContent, Steps(("One", true), ("Two", true))));

		component.FindAll(".step-content").Select(e => e.TextContent).Should().Equal("One content");
	}

	/// <summary>Verifies that moving to the next step swaps which step's content is shown.</summary>
	[Fact]
	public async Task Moving_on_shows_the_next_step_content()
	{
		var component = Render<PDWizard>(parameters => parameters
			.Add(p => p.ChildContent, Steps(("One", true), ("Two", true))));

		await component.InvokeAsync(() => component.Instance.NextAsync());

		component.FindAll(".step-content").Select(e => e.TextContent).Should().Equal("Two content");
	}

	/// <summary>
	/// Verifies that each registered step's title appears in the indicator, and an invisible step is skipped.
	/// </summary>
	[Fact]
	public void Registered_titles_appear_and_invisible_steps_are_skipped()
	{
		var component = Render<PDWizard>(parameters => parameters
			.Add(p => p.ChildContent, Steps(("One", true), ("Hidden", false), ("Three", true))));

		component.FindAll(".pd-wizard-step-label").Select(e => e.TextContent).Should().Equal("One", "Three");
	}

	/// <summary>Verifies that a step rendered outside any wizard renders nothing rather than throwing.</summary>
	[Fact]
	public void A_step_outside_a_wizard_renders_nothing()
	{
		var component = Render<PDWizardStep>(parameters => parameters
			.Add(p => p.Title, "Orphan")
			.Add(p => p.ChildContent, (RenderFragment)(b => b.AddContent(0, "Orphan content"))));

		component.Markup.Trim().Should().BeEmpty();
	}

	private static RenderFragment Steps(params (string Title, bool Visible)[] specs) => builder =>
	{
		var sequence = 0;
		foreach (var (title, visible) in specs)
		{
			builder.OpenComponent<PDWizardStep>(sequence++);
			builder.AddComponentParameter(sequence++, nameof(PDWizardStep.Title), title);
			builder.AddComponentParameter(sequence++, nameof(PDWizardStep.IsVisible), visible);
			builder.AddComponentParameter(sequence++, nameof(PDWizardStep.ChildContent), (RenderFragment)(content =>
			{
				content.OpenElement(0, "span");
				content.AddAttribute(1, "class", "step-content");
				content.AddContent(2, $"{title} content");
				content.CloseElement();
			}));
			builder.CloseComponent();
		}
	};
}
