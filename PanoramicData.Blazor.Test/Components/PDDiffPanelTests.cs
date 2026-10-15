using AwesomeAssertions;
using System.Reflection;
using BlazorMonaco.Editor;
using Bunit;
using Microsoft.JSInterop;

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
		var task = cut.InvokeAsync(() => (Task)init.Invoke(cut.Instance, null)!);
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

	private static readonly MethodInfo _initMethod = typeof(PDDiffPanel).GetMethod("OnEditorInitAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;

	private static T GetField<T>(PDDiffPanel panel, string name)
		=> (T)typeof(PDDiffPanel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!;

	private static void SetField(PDDiffPanel panel, string name, object value)
		=> typeof(PDDiffPanel).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(panel, value);

	private IRenderedComponent<PDDiffPanel> RenderFailingInit(TimeSpan retryDelay)
	{
		JSInterop.Setup<TextModel>(i => i.Identifier.EndsWith("createModel", StringComparison.Ordinal))
			.SetException(new JSException("monaco is not defined"));
		var cut = Render<PDDiffPanel>(p => p.Add(c => c.Original, "a").Add(c => c.Modified, "b"));
		SetField(cut.Instance, "_initRetryDelay", retryDelay);
		return cut;
	}

	/// <summary>Disposing detaches the models from the widget before the models are disposed.</summary>
	[Fact]
	public async Task Dispose_ReleasesEditorBeforeDisposingModels()
	{
		JSInterop.Setup<TextModel>(i => i.Identifier.EndsWith("createModel", StringComparison.Ordinal))
			.SetResult(new TextModel { Id = "m1", Uri = "inmemory://model/1" });
		var module = JSInterop.SetupModule("./_content/PanoramicData.Blazor/PDDiffPanel.razor.js");
		// releaseEditor stays pending until released, so we can observe what dispose does while it is outstanding.
		var release = module.SetupVoid("releaseEditor", _ => true);
		var cut = Render<PDDiffPanel>(p => p.Add(c => c.Original, "a").Add(c => c.Modified, "b"));
		await (Task)_initMethod.Invoke(cut.Instance, null)!;
		bool ModelDisposed() => JSInterop.Invocations.Any(i => i.Identifier.EndsWith("dispose", StringComparison.OrdinalIgnoreCase));
		ModelDisposed().Should().BeFalse();

		var dispose = cut.Instance.DisposeAsync().AsTask();
		await Task.Delay(50, Xunit.TestContext.Current.CancellationToken);

		module.Invocations.Select(i => i.Identifier).Should().Contain("releaseEditor");
		ModelDisposed().Should().BeFalse("the models must not be disposed while the editor is still attached");
		release.SetVoidResult();
		await dispose;
		ModelDisposed().Should().BeTrue();
	}

	// Rendering recreates the editor after a bump, which (in bUnit's loose interop) fires init again, so the retry
	// loop runs by itself; these tests observe it rather than driving it.
	private static async Task WaitForAsync(Func<bool> condition)
	{
		for (var i = 0; i < 500 && !condition(); i++)
		{
			await Task.Delay(10, Xunit.TestContext.Current.CancellationToken);
		}

		condition().Should().BeTrue("the condition should be reached within 5 seconds");
	}

	/// <summary>A JavaScript failure while creating the models (Monaco not loaded yet) bumps the editor generation to retry.</summary>
	[Fact]
	public async Task Init_JSException_RetriesByRecreatingTheEditor()
	{
		var cut = RenderFailingInit(TimeSpan.FromMilliseconds(5));

		await WaitForAsync(() => GetField<int>(cut.Instance, "_editorGeneration") >= 1);

		GetField<int>(cut.Instance, "_initAttempts").Should().BeGreaterThanOrEqualTo(1);
	}

	/// <summary>The retry stops once MaxInitAttempts is exceeded.</summary>
	[Fact]
	public async Task Init_JSException_StopsRetryingAfterMaxAttempts()
	{
		var max = (int)typeof(PDDiffPanel).GetField("MaxInitAttempts", BindingFlags.Static | BindingFlags.NonPublic)!.GetRawConstantValue()!;
		var cut = RenderFailingInit(TimeSpan.FromMilliseconds(1));

		await WaitForAsync(() => GetField<int>(cut.Instance, "_initAttempts") > max);
		await Task.Delay(100, Xunit.TestContext.Current.CancellationToken);

		GetField<int>(cut.Instance, "_editorGeneration").Should().Be(max);
		GetField<int>(cut.Instance, "_initAttempts").Should().Be(max + 1);
	}

	/// <summary>Disposing during the retry delay produces no generation bump.</summary>
	[Fact]
	public async Task Init_JSException_DisposedDuringDelay_DoesNotBump()
	{
		var cut = RenderFailingInit(TimeSpan.FromMilliseconds(200));
		var before = GetField<int>(cut.Instance, "_editorGeneration");

		var init = cut.InvokeAsync(() => (Task)_initMethod.Invoke(cut.Instance, null)!);
		await cut.Instance.DisposeAsync();
		await init;

		GetField<int>(cut.Instance, "_editorGeneration").Should().Be(before);
	}
}
