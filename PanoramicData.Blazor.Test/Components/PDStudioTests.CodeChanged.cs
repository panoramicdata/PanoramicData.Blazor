using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tells the host when the editor's code changes, so it can warn before that code is lost.
/// </summary>
public partial class PDStudioTests
{
	private IRenderedComponent<PDStudio> RenderStudioReporting(List<string> changes)
		=> RenderStudio(more: p => p.Add(x => x.CodeChanged, EventCallback.Factory.Create<string>(this, changes.Add)));

	/// <summary>
	/// Verifies that typing reports the new code, which is then readable as <see cref="PDStudio.Code"/>.
	/// </summary>
	[Fact]
	public async Task CodeChanged_IsRaisedWhenTheUserTypes()
	{
		var changes = new List<string>();
		var studio = RenderStudioReporting(changes);

		await TypeAsync(studio, "<p>Hi</p>");

		changes.Should().Equal("<p>Hi</p>");
		studio.Instance.Code.Should().Be("<p>Hi</p>");
	}

	/// <summary>
	/// Verifies that New reports the code as empty, since it replaces what was there.
	/// </summary>
	[Fact]
	public async Task CodeChanged_IsRaisedByNew()
	{
		var changes = new List<string>();
		var studio = RenderStudioReporting(changes);
		await TypeAsync(studio, "<p>Hi</p>");

		await ToolbarButton(studio, "new").ClickAsync(new MouseEventArgs());

		changes.Should().Equal("<p>Hi</p>", string.Empty);
		studio.Instance.Code.Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that loading an example reports the example, since it replaces what was there.
	/// </summary>
	[Fact]
	public async Task CodeChanged_IsRaisedByLoadingAnExample()
	{
		var changes = new List<string>();
		var studio = RenderStudio(more: p => p
			.Add(x => x.CodeChanged, EventCallback.Factory.Create<string>(this, changes.Add))
			.Add(x => x.Examples, [new PDStudioExample("Greeting", "<p>Hello</p>")]));

		await MenuAsync(studio, "Examples", "example-0");

		changes.Should().Equal("<p>Hello</p>");
	}
}
