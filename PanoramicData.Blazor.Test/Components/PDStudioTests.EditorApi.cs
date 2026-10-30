using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// What a host can do to the editor and toolbar: set the code, put content before Execute, and leave out New.
/// </summary>
public partial class PDStudioTests
{
	/// <summary>
	/// Verifies that code a host sets is reported, readable and what Execute then runs.
	/// </summary>
	[Fact]
	public async Task SetCodeAsync_ReplacesTheCode_AndExecuteRunsIt()
	{
		var changes = new List<string>();
		var studio = RenderStudio(more: p => p.Add(x => x.CodeChanged, EventCallback.Factory.Create<string>(this, changes.Add)));
		await TypeAsync(studio, "SELECT 1");

		await studio.InvokeAsync(() => studio.Instance.SetCodeAsync("SELECT 2"));
		await PlayButton(studio).ClickAsync(new MouseEventArgs());

		changes.Should().Equal("SELECT 1", "SELECT 2");
		studio.Instance.Code.Should().Be("SELECT 2");
		studio.WaitForAssertion(() => _service.Calls.Should().ContainSingle().Which.Code.Should().Be("SELECT 2"));
	}

	/// <summary>
	/// Verifies that the host's start content comes before Execute.
	/// </summary>
	[Fact]
	public void ToolbarStartContent_ComesBeforeExecute()
	{
		var studio = RenderStudio(more: p => p
			.Add(x => x.ToolbarStartContent, b => b.AddMarkupContent(0, "<span class=\"file-menu\">File</span>")));

		var toolbar = studio.Find(".pd-studio-toolbar").InnerHtml;
		toolbar.IndexOf("file-menu", StringComparison.Ordinal).Should()
			.BeLessThan(toolbar.IndexOf("pdtoolbarbutton", StringComparison.Ordinal));
	}

	/// <summary>
	/// Verifies that New can be left out, and that no separator is left dangling after Execute when nothing follows it.
	/// </summary>
	[Fact]
	public void ShowNewButton_False_LeavesOutNewAndItsSeparator()
	{
		var withoutNew = RenderStudio(new PDStudioOptions { ShowNewButton = false });

		withoutNew.FindComponents<PDToolbarButton>().Select(b => b.Instance.Key).Should().Equal("play");
		withoutNew.FindComponents<PDToolbarSeparator>().Should().BeEmpty();

		var withExamples = RenderStudio(new PDStudioOptions { ShowNewButton = false, Language = "sql" },
			p => p.Add(x => x.Examples, [new PDStudioExample("One", "SELECT 1")]));

		withExamples.FindComponents<PDToolbarSeparator>().Should().ContainSingle();
		withExamples.FindComponents<PDToolbarDropdown>().Should().ContainSingle();
	}
}
