using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDWizard"/> shows one step at a time, moves between steps through its footer and
/// indicator, and runs the steps' enter and leave hooks.
/// </summary>
public class PDWizardTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDWizardTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	private IRenderedComponent<PDWizard> RenderWizard(IEnumerable<Step> steps, Action<ComponentParameterCollectionBuilder<PDWizard>>? configure = null)
		=> Render<PDWizard>(parameters =>
		{
			parameters.Add(p => p.ChildContent, StepsFragment(steps));
			configure?.Invoke(parameters);
		});

	private static RenderFragment StepsFragment(IEnumerable<Step> steps) => builder =>
	{
		foreach (var step in steps)
		{
			builder.OpenComponent<PDWizardStep>(0);
			builder.SetKey(step.Title);
			builder.AddAttribute(1, nameof(PDWizardStep.Title), step.Title);
			builder.AddAttribute(2, nameof(PDWizardStep.Icon), step.Icon);
			builder.AddAttribute(3, nameof(PDWizardStep.IsVisible), step.IsVisible);
			builder.AddAttribute(4, nameof(PDWizardStep.CanProceed), step.CanProceed);
			builder.AddAttribute(5, nameof(PDWizardStep.OnEnterAsync), step.OnEnterAsync);
			builder.AddAttribute(6, nameof(PDWizardStep.OnLeaveAsync), step.OnLeaveAsync);
			builder.AddAttribute(7, nameof(PDWizardStep.LoadingContent), step.LoadingContent);
			builder.AddAttribute(8, nameof(PDWizardStep.ChildContent), (RenderFragment)(b => b.AddMarkupContent(0, $"<p class=\"body\">{step.Title} body</p>")));
			builder.CloseComponent();
		}
	};

	private static Step[] ThreeSteps() => [new("One"), new("Two"), new("Three")];

	private static IElement FooterButton(IRenderedComponent<PDWizard> wizard, string text)
		=> wizard.FindAll(".pd-wizard-footer button").Single(b => b.TextContent.Trim() == text);

	private static string Body(IRenderedComponent<PDWizard> wizard) => wizard.Find("p.body").TextContent;

	/// <summary>
	/// Verifies that only the first step's content is shown at first, with no Back button and Next enabled.
	/// </summary>
	[Fact]
	public void Initially_ShowsTheFirstStepOnly()
	{
		var wizard = RenderWizard(ThreeSteps(), p => p.Add(x => x.CssClass, "setup").AddUnmatched("data-kind", "wizard"));

		wizard.FindAll("p.body").Should().ContainSingle();
		Body(wizard).Should().Be("One body");
		wizard.Find("div.pd-wizard").ClassList.Should().Contain(["setup", "pd-wizard--navigable"]);
		wizard.Find("div.pd-wizard").GetAttribute("data-kind").Should().Be("wizard");
		wizard.FindAll(".pd-wizard-footer button").Select(b => b.TextContent.Trim()).Should().Equal("Cancel", "Next");
		wizard.Instance.CurrentStep!.Title.Should().Be("One");
	}

	/// <summary>
	/// Verifies that Next and Back move through the steps, raising StepChanged, with Finish on the last step.
	/// </summary>
	[Fact]
	public void NextAndBack_MoveBetweenSteps()
	{
		var changes = new List<int>();
		var wizard = RenderWizard(ThreeSteps(), p => p.Add(x => x.StepChanged, i => changes.Add(i)));

		FooterButton(wizard, "Next").Click();
		Body(wizard).Should().Be("Two body");
		FooterButton(wizard, "Next").Click();
		Body(wizard).Should().Be("Three body");
		wizard.FindAll(".pd-wizard-footer button").Select(b => b.TextContent.Trim()).Should().Equal("Cancel", "Back", "Finish");

		FooterButton(wizard, "Back").Click();

		Body(wizard).Should().Be("Two body");
		changes.Should().Equal(1, 2, 1);
		wizard.Instance.CurrentStepIndex.Should().Be(1);
	}

	/// <summary>
	/// Verifies that a step that cannot proceed disables Next, and on the last step disables Finish.
	/// </summary>
	[Fact]
	public async Task CanProceedFalse_DisablesNextAndFinish()
	{
		var wizard = RenderWizard([new("One") { CanProceed = () => false }, new("Two") { CanProceed = () => false }]);

		FooterButton(wizard, "Next").HasAttribute("disabled").Should().BeTrue();
		await wizard.InvokeAsync(() => wizard.Instance.GoToStepAsync(1));

		FooterButton(wizard, "Finish").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>
	/// Verifies that finishing runs the last step's leave hook, raises OnComplete and returns to the first step.
	/// </summary>
	[Fact]
	public void Finish_RaisesOnComplete_AndResets()
	{
		var events = new List<string>();
		Step[] steps = [new("One"), new("Two") { OnLeaveAsync = () => { events.Add("leave"); return Task.CompletedTask; } }];
		var wizard = RenderWizard(steps, p => p.Add(x => x.OnComplete, () => events.Add("complete")));
		FooterButton(wizard, "Next").Click();

		FooterButton(wizard, "Finish").Click();

		events.Should().Equal("leave", "complete");
		Body(wizard).Should().Be("One body");
	}

	/// <summary>
	/// Verifies that cancelling raises OnCancel and returns to the first step.
	/// </summary>
	[Fact]
	public void Cancel_RaisesOnCancel_AndResets()
	{
		var cancelled = 0;
		var changes = new List<int>();
		var wizard = RenderWizard(ThreeSteps(), p => p
			.Add(x => x.OnCancel, () => cancelled++)
			.Add(x => x.StepChanged, i => changes.Add(i))
			.Add(x => x.CancelIcon, "fa fa-times"));
		FooterButton(wizard, "Next").Click();

		FooterButton(wizard, "Cancel").Click();

		cancelled.Should().Be(1);
		Body(wizard).Should().Be("One body");
		changes.Should().Equal(1, 0);
		wizard.Find(".pd-wizard-footer i.fa-times").Should().NotBeNull();
	}

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

	/// <summary>
	/// Verifies that the numbers indicator marks steps completed, active and pending, with a check on
	/// completed steps, the step's own icon where it has one, and connectors between steps.
	/// </summary>
	[Fact]
	public async Task NumbersIndicator_ShowsEachStepsState()
	{
		var wizard = RenderWizard([new("One"), new("Two") { Icon = "fa fa-star" }, new("Three")]);
		await wizard.InvokeAsync(() => wizard.Instance.GoToStepAsync(2));

		var nodes = wizard.FindAll(".pd-wizard-step-node");
		nodes.Select(n => n.ClassList.Single(c => c.StartsWith("pd-wizard-step-node--", StringComparison.Ordinal)))
			.Should().Equal("pd-wizard-step-node--completed", "pd-wizard-step-node--completed", "pd-wizard-step-node--active");
		nodes[0].QuerySelector("i")!.ClassName.Should().Be("fa fa-solid fa-check");
		nodes[1].QuerySelector("i")!.ClassName.Should().Be("fa fa-star");
		nodes[2].TextContent.Should().Contain("3").And.Contain("Three");
		wizard.FindAll(".pd-wizard-connector--completed").Should().HaveCount(2);
	}

	/// <summary>
	/// Verifies that the dots and breadcrumb indicators render one marker per visible step in its state.
	/// </summary>
	[Fact]
	public void DotsAndBreadcrumbIndicators_RenderEachStep()
	{
		var dots = RenderWizard(ThreeSteps(), p => p.Add(x => x.StepIndicatorStyle, WizardStepIndicatorStyle.Dots));
		dots.FindAll(".pd-wizard-dot").Select(d => d.GetAttribute("title")).Should().Equal("One", "Two", "Three");
		dots.Find(".pd-wizard-dot").ClassList.Should().Contain("pd-wizard-dot--active");

		var crumbs = RenderWizard([new("One") { Icon = "fa fa-home" }, new("Two")], p => p.Add(x => x.StepIndicatorStyle, WizardStepIndicatorStyle.Breadcrumb));

		crumbs.FindAll(".pd-wizard-crumb").Select(c => c.TextContent.Trim()).Should().Equal("One", "Two");
		crumbs.Find(".pd-wizard-crumb i.fa-home").Should().NotBeNull();
		crumbs.FindAll(".pd-wizard-crumb-sep").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that clicking a completed step in the indicator goes back to it, but a pending step cannot be
	/// jumped to.
	/// </summary>
	[Fact]
	public async Task IndicatorClick_GoesBackButNotForward()
	{
		var wizard = RenderWizard(ThreeSteps());
		await wizard.InvokeAsync(() => wizard.Instance.GoToStepAsync(1));

		wizard.FindAll(".pd-wizard-step-node")[2].Click();
		Body(wizard).Should().Be("Two body");

		wizard.FindAll(".pd-wizard-step-node")[0].Click();
		Body(wizard).Should().Be("One body");
	}

	/// <summary>
	/// Verifies that with step navigation off the indicator cannot be used to go back.
	/// </summary>
	[Fact]
	public async Task IndicatorClick_WithNavigationOff_DoesNothing()
	{
		var wizard = RenderWizard(ThreeSteps(), p => p.Add(x => x.AllowStepNavigation, false));
		await wizard.InvokeAsync(() => wizard.Instance.GoToStepAsync(1));

		wizard.FindAll(".pd-wizard-step-node")[0].Click();

		Body(wizard).Should().Be("Two body");
		wizard.Find("div.pd-wizard").ClassList.Should().NotContain("pd-wizard--navigable");
	}

	/// <summary>
	/// Verifies that hidden steps are skipped by the indicator and by navigation.
	/// </summary>
	[Fact]
	public void HiddenSteps_AreSkipped()
	{
		var wizard = RenderWizard([new("One"), new("Hidden") { IsVisible = false }, new("Three")]);

		wizard.FindAll(".pd-wizard-step-node").Should().HaveCount(2);
		FooterButton(wizard, "Next").Click();

		Body(wizard).Should().Be("Three body");
	}

	/// <summary>
	/// Verifies that navigating outside the steps, or back from the first, is ignored.
	/// </summary>
	[Fact]
	public async Task OutOfRangeNavigation_IsIgnored()
	{
		var changes = new List<int>();
		var wizard = RenderWizard(ThreeSteps(), p => p.Add(x => x.StepChanged, i => changes.Add(i)));

		await wizard.InvokeAsync(() => wizard.Instance.GoToStepAsync(5));
		await wizard.InvokeAsync(() => wizard.Instance.GoToStepAsync(-1));
		await wizard.InvokeAsync(() => wizard.Instance.BackAsync());

		changes.Should().BeEmpty();
		await wizard.InvokeAsync(() => wizard.Instance.GoToStepAsync(2));
		await wizard.InvokeAsync(() => wizard.Instance.NextAsync());
		changes.Should().Equal(2);
	}

	/// <summary>
	/// Verifies that the indicator is hidden for a single step or when switched off, and the footer can be
	/// hidden or replaced.
	/// </summary>
	[Fact]
	public void IndicatorAndFooter_CanBeHiddenOrReplaced()
	{
		var single = RenderWizard([new("Only")]);
		single.FindAll(".pd-wizard-indicator").Should().BeEmpty();

		var noIndicator = RenderWizard(ThreeSteps(), p => p.Add(x => x.ShowIndicator, false).Add(x => x.ShowFooter, false));
		noIndicator.FindAll(".pd-wizard-indicator").Should().BeEmpty();
		noIndicator.FindAll(".pd-wizard-footer").Should().BeEmpty();

		var custom = RenderWizard(ThreeSteps(), p => p.Add(x => x.Footer, "<span class=\"my-footer\">Mine</span>"));
		custom.Find(".pd-wizard-footer span.my-footer").TextContent.Should().Be("Mine");
		custom.FindAll(".pd-wizard-footer button").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the title template, the title addon and the extra button are rendered with the current step.
	/// </summary>
	[Fact]
	public void TitleBarAndExtraButton_AreRendered()
	{
		var wizard = RenderWizard(ThreeSteps(), p => p
			.Add(x => x.TitleTemplate, step => $"<h3>{step?.Title}</h3>")
			.Add(x => x.TitleAddon, step => $"<small>Step {step?.Title}</small>")
			.Add(x => x.ExtraButton, "<button class=\"draft\">Save draft</button>")
			.Add(x => x.ShowStepTitles, false));

		wizard.Find(".pd-wizard-titlebar-main h3").TextContent.Should().Be("One");
		wizard.Find(".pd-wizard-titlebar-addon.text-secondary small").TextContent.Should().Be("Step One");
		wizard.Find(".pd-wizard-footer button.draft").TextContent.Should().Be("Save draft");
		wizard.FindAll(".pd-wizard-step-label").Should().BeEmpty();
	}

	/// <summary>A step definition for the wizard under test.</summary>
	/// <param name="Title">Step title.</param>
	private sealed record Step(string Title)
	{
		public string? Icon { get; init; }

		public bool IsVisible { get; init; } = true;

		public Func<bool>? CanProceed { get; init; }

		public Func<Task>? OnEnterAsync { get; init; }

		public Func<Task>? OnLeaveAsync { get; init; }

		public RenderFragment? LoadingContent { get; init; }
	}
}
