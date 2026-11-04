using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// A menu a host joins to Execute, as a split button.
/// </summary>
public partial class PDStudioTests
{
	private static List<MenuItem> RunOptions() =>
	[
		new MenuItem { Key = "fast", Text = "Fast" },
		new MenuItem { Key = "thorough", Text = "Thorough" }
	];

	private static PDToolbarDropdown? ExecuteMenu(IRenderedComponent<PDStudio> studio)
		=> studio.FindComponents<PDToolbarDropdown>().SingleOrDefault(d => d.Instance.Key == "execute-menu")?.Instance;

	/// <summary>Verifies that Execute has no menu unless the host gives it items.</summary>
	[Fact]
	public void ExecuteMenu_WithoutItems_IsNotShown()
	{
		ExecuteMenu(RenderStudio()).Should().BeNull();
		ExecuteMenu(RenderStudio(more: p => p.Add(x => x.ExecuteMenuItems, []))).Should().BeNull();
	}

	/// <summary>
	/// Verifies that the menu sits straight after Execute, before the host's own group items and Cancel, as a caret
	/// in Execute's colour with the host's tooltip.
	/// </summary>
	[Fact]
	public void ExecuteMenu_IsACaretJoinedToExecute_InItsColour()
	{
		var studio = RenderStudio(new PDStudioOptions { ShowCancelButton = true, ExecuteButtonCssClass = "btn-primary" }, p => p
			.Add(x => x.ExecuteMenuItems, RunOptions())
			.Add(x => x.ExecuteMenuToolTip, "Run options")
			.Add(x => x.ExecuteGroupContent, b => b.AddMarkupContent(0, "<span class=\"explain\">Explain</span>")));

		var group = studio.Find(".pd-studio-execute-group");
		var toggle = group.QuerySelector("button.dropdown-toggle")!;
		toggle.ClassList.Should().Contain(["pd-studio-execute-menu", "btn-primary"]);
		toggle.GetAttribute("title").Should().Be("Run options");
		toggle.TextContent.Trim().Should().BeEmpty("it is a caret alone");
		ButtonTexts(group).Should().Equal("Execute", string.Empty, "Cancel");
		group.InnerHtml.IndexOf("pd-studio-execute-menu", StringComparison.Ordinal)
			.Should().BeLessThan(group.InnerHtml.IndexOf("explain", StringComparison.Ordinal));
		ExecuteMenu(studio)!.Items.Select(item => item.Text).Should().Equal("Fast", "Thorough");
	}

	/// <summary>Verifies that choosing an item raises its key to the host.</summary>
	[Fact]
	public async Task ExecuteMenu_RaisesTheChosenItemsKey()
	{
		var chosen = new List<string>();
		var studio = RenderStudio(more: p => p
			.Add(x => x.ExecuteMenuItems, RunOptions())
			.Add(x => x.ExecuteMenuClick, key => chosen.Add(key)));

		await studio.InvokeAsync(() => ExecuteMenu(studio)!.Click.InvokeAsync("thorough"));

		chosen.Should().Equal("thorough");
	}

	/// <summary>Verifies that while Execute is Cancel, its menu takes Cancel's colour with it.</summary>
	[Fact]
	public async Task ExecuteMenu_TakesCancelsColour_WhileExecuteIsCancel()
	{
		var studio = RenderStudio(more: p => p.Add(x => x.ExecuteMenuItems, RunOptions()));
		_service.Block = true;
		await TypeAsync(studio, "sleep(10)");

		var running = PlayButton(studio).ClickAsync(new MouseEventArgs());
		studio.WaitForAssertion(() => studio.Find("button.pd-studio-execute-menu").ClassList.Should().Contain("btn-danger").And.NotContain("btn-success"));

		await PlayButton(studio).ClickAsync(new MouseEventArgs());
		await running;
		studio.WaitForAssertion(() => studio.Find("button.pd-studio-execute-menu").ClassList.Should().Contain("btn-success"));
	}

	private static List<string> ButtonTexts(AngleSharp.Dom.IElement element)
		=> [.. element.QuerySelectorAll("button").Select(button => button.TextContent.Trim())];
}
