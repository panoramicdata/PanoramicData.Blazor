using AwesomeAssertions;
using BlazorMonaco.Editor;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Examples, New and editor tests for <see cref="PDStudio"/>.
/// </summary>
public partial class PDStudioTests
{
	private static readonly PDStudioExample[] _examples =
	[
		new("Any language", "-- shared"),
		new("SQL only", "SELECT 1", "sql"),
		new("NCalc only", "1 + 1", "ncalc")
	];

	/// <summary>
	/// Verifies that the Examples menu offers only the examples for the studio's language, and loads the chosen one.
	/// </summary>
	[Fact]
	public async Task ExamplesMenu_OffersTheLanguagesExamplesAndLoadsTheChosenOne()
	{
		var studio = RenderStudio(new PDStudioOptions { Language = "SQL" }, p => p.Add(x => x.Examples, _examples));

		studio.FindComponents<PDMenuItem>().Select(m => m.Instance.Text).Should().Equal("Any language", "SQL only");

		await MenuAsync(studio, "Examples", "example-1");
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		studio.WaitForAssertion(() => _service.Calls.Should().ContainSingle().Which.Code.Should().Be("SELECT 1"));
		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain("Example loaded: SQL only"));
	}

	/// <summary>
	/// Verifies that there is no Examples menu when none of the examples is for the studio's language.
	/// </summary>
	[Fact]
	public void ExamplesMenu_NoneForTheLanguage_IsLeftOut()
	{
		var studio = RenderStudio(new PDStudioOptions { Language = "html" }, p => p.Add(x => x.Examples, _examples[1..]));

		studio.FindComponents<PDToolbarDropdown>().Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that a key naming no example changes nothing.
	/// </summary>
	[Theory]
	[InlineData("example-9")]
	[InlineData("example-x")]
	[InlineData("other")]
	public async Task ExamplesMenu_UnknownKey_ChangesNothing(string key)
	{
		var studio = RenderStudio(new PDStudioOptions { Language = "sql" }, p => p.Add(x => x.Examples, _examples));
		await TypeAsync(studio, "SELECT 2");

		await MenuAsync(studio, "Examples", key);

		studio.Instance.Code.Should().Be("SELECT 2");
	}

	/// <summary>
	/// Verifies that New clears the code and results, so there is then nothing to execute.
	/// </summary>
	[Fact]
	public async Task New_ClearsTheCode()
	{
		var studio = RenderStudio();
		await TypeAsync(studio, "<p>Hi</p>");

		await ToolbarButton(studio, "new").ClickAsync(new MouseEventArgs());
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		_service.Calls.Should().BeEmpty();
		studio.WaitForAssertion(() => LogMessages(studio).Should().Contain("New document created"));
		studio.FindAll(".pd-studio-results-placeholder").Should().ContainSingle();
	}
	/// <summary>
	/// Verifies that the editor is configured with the studio's defaults, then the caller's own adjustments.
	/// </summary>
	[Fact]
	public void EditorOptions_ApplyTheDefaultsThenTheCallersAdjustments()
	{
		var studio = RenderStudio(more: p => p
			.Add(x => x.InitializeMonacoOptions, o => o.WordWrap = "off"));
		var options = new StandaloneEditorConstructionOptions();

		studio.FindComponent<PDMonacoEditor>().Instance.InitializeOptions!(options);

		options.AutomaticLayout.Should().BeTrue();
		options.ScrollBeyondLastLine.Should().BeFalse();
		options.Minimap!.Enabled.Should().BeFalse();
		options.Folding.Should().BeTrue();
		options.WordWrap.Should().Be("off");
	}

	/// <summary>
	/// Verifies that the editor is given the studio's language and theme.
	/// </summary>
	[Fact]
	public void Editor_UsesTheLanguageAndTheme()
	{
		var studio = RenderStudio(new PDStudioOptions { Language = "sql", Theme = "vs-dark" });

		var editor = studio.FindComponent<PDMonacoEditor>().Instance;
		editor.Language.Should().Be("sql");
		editor.Theme.Should().Be("vs-dark");
	}
}
