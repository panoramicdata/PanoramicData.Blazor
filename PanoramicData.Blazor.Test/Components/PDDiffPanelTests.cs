using AwesomeAssertions;
using System.Reflection;
using BlazorMonaco.Editor;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests the parameter logic of <see cref="PDDiffPanel"/>: language resolution, the "No changes" state and the
/// layout toggle.
/// </summary>
/// <remarks>
/// Monaco itself runs only in a browser, so these tests answer its JavaScript calls with bUnit's loose interop
/// and do not exercise the diff rendering.
/// </remarks>
public class PDDiffPanelTests : BunitContext
{
	/// <summary>Sets up the rendering context with loose JavaScript interop.</summary>
	public PDDiffPanelTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>An explicit language wins over the one inferred from the file name.</summary>
	[Fact]
	public void EffectiveLanguage_UsesExplicitLanguageFirst()
	{
		var cut = Render<PDDiffPanel>(p => p
			.Add(c => c.FileName, "a.cs")
			.Add(c => c.Language, "json"));
		cut.Instance.EffectiveLanguage.Should().Be("json");
	}

	/// <summary>Without an explicit language the file name decides.</summary>
	[Fact]
	public void EffectiveLanguage_FallsBackToFileName()
	{
		var cut = Render<PDDiffPanel>(p => p.Add(c => c.FileName, "a.cs"));
		cut.Instance.EffectiveLanguage.Should().Be("csharp");
	}

	/// <summary>Identical texts show the "No changes" state.</summary>
	[Fact]
	public void HasChanges_FalseWhenIdentical_ShowsNoChanges()
	{
		var cut = Render<PDDiffPanel>(p => p
			.Add(c => c.Original, "same")
			.Add(c => c.Modified, "same"));
		cut.Instance.HasChanges.Should().BeFalse();
		cut.Markup.Should().Contain("No changes");
	}

	/// <summary>Different texts hide the "No changes" state.</summary>
	[Fact]
	public void HasChanges_TrueWhenDifferent_HidesNoChanges()
	{
		var cut = Render<PDDiffPanel>(p => p
			.Add(c => c.Original, "a")
			.Add(c => c.Modified, "b"));
		cut.Instance.HasChanges.Should().BeTrue();
		cut.Markup.Should().NotContain("No changes");
	}

	/// <summary>Clicking the toggle switches between side-by-side and inline, and the label follows.</summary>
	[Fact]
	public void SideBySideToggle_FlipsOnButtonClick()
	{
		var cut = Render<PDDiffPanel>(p => p.Add(c => c.Original, "a").Add(c => c.Modified, "b"));
		cut.Find(".pd-diffpanel-toggle").TextContent.Trim().Should().Be("Inline");
		cut.Find(".pd-diffpanel-toggle").Click();
		cut.Find(".pd-diffpanel-toggle").TextContent.Trim().Should().Be("Side by side");
	}

	/// <summary>A user toggle survives a parent re-render that leaves SideBySide unchanged.</summary>
	[Fact]
	public void SideBySideToggle_SurvivesRerenderWithSameParameter()
	{
		var cut = Render<PDDiffPanel>(p => p.Add(c => c.SideBySide, true).Add(c => c.Modified, "b"));
		cut.Find(".pd-diffpanel-toggle").Click();
		cut.Render(p => p.Add(c => c.SideBySide, true).Add(c => c.Modified, "c"));
		cut.Find(".pd-diffpanel-toggle").TextContent.Trim().Should().Be("Side by side");
	}

	/// <summary>A parent changing SideBySide after init takes effect.</summary>
	[Fact]
	public void SideBySide_ParentChangeIsApplied()
	{
		var cut = Render<PDDiffPanel>(p => p.Add(c => c.SideBySide, true));
		cut.Render(p => p.Add(c => c.SideBySide, false));
		cut.Find(".pd-diffpanel-toggle").TextContent.Trim().Should().Be("Side by side");
	}

	/// <summary>Parameters that change while the editor is still creating its models reach the models.</summary>
	[Fact]
	public async Task Init_RaceWithParameterChange_PushesLatestText()
	{
		var createModel = JSInterop.Setup<TextModel>(i => i.Identifier.EndsWith("createModel", StringComparison.Ordinal));
		var cut = Render<PDDiffPanel>(p => p.Add(c => c.Original, "old").Add(c => c.Modified, "old"));
		var init = typeof(PDDiffPanel).GetMethod("OnEditorInitAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

		// Init is now suspended at its first createModel call; change the parameters while it waits.
		var task = (Task)init.Invoke(cut.Instance, null)!;
		task.IsCompleted.Should().BeFalse();
		cut.Render(p => p.Add(c => c.Original, "new-original").Add(c => c.Modified, "new-modified"));
		createModel.SetResult(new TextModel { Id = "m1", Uri = "inmemory://model/1" });
		await task;

		var setValues = JSInterop.Invocations
			.Where(i => i.Identifier.EndsWith("setValue", StringComparison.Ordinal))
			.SelectMany(i => i.Arguments.OfType<string>())
			.ToList();
		setValues.Should().Contain("new-original").And.Contain("new-modified");
	}


	/// <summary>A ReadOnly change after init is pushed to the live editor.</summary>
	[Fact]
	public async Task ReadOnly_ChangeAfterInit_IsPushedToEditor()
	{
		JSInterop.Setup<TextModel>(i => i.Identifier.EndsWith("createModel", StringComparison.Ordinal))
			.SetResult(new TextModel { Id = "m1", Uri = "inmemory://model/1" });
		var cut = Render<PDDiffPanel>(p => p.Add(c => c.ReadOnly, true));
		var init = typeof(PDDiffPanel).GetMethod("OnEditorInitAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
		await (Task)init.Invoke(cut.Instance, null)!;
		cut.Render(p => p.Add(c => c.ReadOnly, false));
		JSInterop.Invocations.Count(i => i.Identifier.EndsWith("updateOptions", StringComparison.Ordinal)).Should().BeGreaterThan(0);
	}
}
