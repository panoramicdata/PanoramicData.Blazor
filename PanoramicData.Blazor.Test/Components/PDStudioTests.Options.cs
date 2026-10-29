using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Toolbar button and results styling options for <see cref="PDStudio"/>.
/// </summary>
public partial class PDStudioTests
{
	/// <summary>
	/// Verifies that each button takes its text, class and size from the options.
	/// </summary>
	[Fact]
	public void Buttons_TakeTheirTextClassAndSizeFromTheOptions()
	{
		var studio = RenderStudio(
			new PDStudioOptions
			{
				ButtonSize = ButtonSizes.Large,
				ExecuteButtonText = "Run",
				ExecuteButtonCssClass = "btn-primary",
				NewButtonText = "Clear",
				NewButtonCssClass = "btn-light",
				ExamplesButtonText = "Samples",
				ExamplesButtonCssClass = "btn-info",
				ButtonTextCssClass = "ps-3"
			},
			p => p.Add(x => x.Examples, [new PDStudioExample("One", "1")]));

		var play = studio.FindComponents<PDToolbarButton>().Single(b => b.Instance.Key == "play").Instance;
		play.Text.Should().Be("Run");
		play.CssClass.Should().Be("btn-primary");
		play.Size.Should().Be(ButtonSizes.Large);
		play.TextCssClass.Should().Be("ps-3");

		var newButton = studio.FindComponents<PDToolbarButton>().Single(b => b.Instance.Key == "new").Instance;
		newButton.Text.Should().Be("Clear");
		newButton.CssClass.Should().Be("btn-light");

		var examples = studio.FindComponent<PDToolbarDropdown>().Instance;
		examples.Text.Should().Be("Samples");
		examples.CssClass.Should().Be("btn-info");
		examples.Size.Should().Be(ButtonSizes.Large);
	}

	/// <summary>
	/// Verifies that the results follow a dark editor theme, and that a host's own stylesheet replaces the built-in one.
	/// </summary>
	[Theory]
	[InlineData("vs-dark", null, "dark", "--text: #dee2e6")]
	[InlineData("sql-light", null, "light", "--text: #212529")]
	[InlineData("vs-dark", "body { color: hotpink; }", "light", "color: hotpink")]
	public async Task Results_FollowTheThemeOrTheHostsStyleSheet(string theme, string? styleSheet, string scheme, string expected)
	{
		var studio = RenderStudio(new PDStudioOptions { Theme = theme, ResultsStyleSheet = styleSheet });

		await RaiseAsync(studio, StudioExecutionEventType.UpdateOutput, output: "<p>Done</p>");

		studio.WaitForAssertion(() => studio.Find("iframe").ClassList.Should().Contain($"scheme-{scheme}"));
		var document = studio.Find("iframe").GetAttribute("srcdoc");
		document.Should().Contain($"<meta name=\"color-scheme\" content=\"{scheme}\">").And.Contain(expected);
	}
}
