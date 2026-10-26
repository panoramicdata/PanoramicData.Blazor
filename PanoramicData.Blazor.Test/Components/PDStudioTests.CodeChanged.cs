using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

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

		await MenuAsync(studio, "File", "New");

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
		var studio = RenderStudioReporting(changes);

		await MenuAsync(studio, "File", "LoadExample1");

		changes.Should().ContainSingle().Which.Should().NotBeNullOrWhiteSpace();
	}
}
