using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Step indicator, title bar and footer tests for <see cref="PDWizard"/>.
/// </summary>
public partial class PDWizardTests
{
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
}
