using AwesomeAssertions;
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

	/// <summary>Clicking the toggle switches between side-by-side and inline.</summary>
	[Fact]
	public void SideBySideToggle_FlipsOnButtonClick()
	{
		var cut = Render<PDDiffPanel>(p => p.Add(c => c.Original, "a").Add(c => c.Modified, "b"));
		cut.Instance.SideBySide.Should().BeTrue();
		cut.Find(".pd-diffpanel-toggle").Click();
		cut.Instance.SideBySide.Should().BeFalse();
	}
}
