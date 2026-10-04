using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Step enter and leave hook tests for <see cref="PDWizard"/>.
/// </summary>
public partial class PDWizardTests
{
	/// <summary>
	/// Verifies that Next runs the current step's leave hook before moving on.
	/// </summary>
	[Fact]
	public void Next_RunsTheLeaveHookFirst()
	{
		string? bodyWhenLeaving = null;
		IRenderedComponent<PDWizard>? wizard = null;
		Step[] steps = [new("One") { OnLeaveAsync = () => { bodyWhenLeaving = Body(wizard!); return Task.CompletedTask; } }, new("Two")];
		wizard = RenderWizard(steps);

		FooterButton(wizard, "Next").Click();

		bodyWhenLeaving.Should().Be("One body");
		Body(wizard).Should().Be("Two body");
	}

	/// <summary>
	/// Verifies that while a step's enter hook runs the body is hidden behind a loading panel and the footer
	/// is disabled, and that both return when it completes.
	/// </summary>
	[Fact]
	public async Task EnterHook_ShowsLoadingUntilItCompletes()
	{
		var entering = new TaskCompletionSource();
		var wizard = RenderWizard([new("One"), new("Two") { OnEnterAsync = () => entering.Task }]);

		FooterButton(wizard, "Next").Click();

		wizard.Find(".pd-wizard-loading").TextContent.Should().Contain("Please wait...");
		wizard.Find(".pd-wizard-body-content").ClassList.Should().Contain("pd-wizard-body-content--hidden");
		wizard.FindAll(".pd-wizard-footer button").Should().OnlyContain(b => b.HasAttribute("disabled"));

		await wizard.InvokeAsync(entering.SetResult);

		wizard.WaitForAssertion(() => wizard.FindAll(".pd-wizard-loading").Should().BeEmpty());
		Body(wizard).Should().Be("Two body");
	}

	/// <summary>
	/// Verifies that a step's own loading content replaces the default spinner.
	/// </summary>
	[Fact]
	public async Task EnterHook_ShowsTheStepsOwnLoadingContent()
	{
		var entering = new TaskCompletionSource();
		RenderFragment loading = b => b.AddMarkupContent(0, "<em class=\"custom-loading\">Fetching</em>");
		var wizard = RenderWizard([new("One"), new("Two") { OnEnterAsync = () => entering.Task, LoadingContent = loading }]);

		FooterButton(wizard, "Next").Click();

		wizard.Find(".pd-wizard-loading em.custom-loading").TextContent.Should().Be("Fetching");
		await wizard.InvokeAsync(entering.SetResult);
	}
}
