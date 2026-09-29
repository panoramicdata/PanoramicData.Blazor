using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="DeleteArgs"/>.</summary>
public class DeleteArgsTests
{
	/// <summary>A new instance has no items and defaults to prompting the user.</summary>
	[Fact]
	public void New_HasNoItemsAndPromptResolution()
	{
		var args = new DeleteArgs();

		args.Items.Should().BeEmpty();
		args.Resolution.Should().Be(DeleteArgs.DeleteResolutions.Prompt);
	}

	/// <summary>Items and resolution round-trip.</summary>
	[Fact]
	public void ItemsAndResolution_RoundTrip()
	{
		var item = new FileExplorerItem { Path = "/a.txt" };

		var args = new DeleteArgs { Items = [item], Resolution = DeleteArgs.DeleteResolutions.Delete };

		args.Items.Should().ContainSingle().Which.Should().BeSameAs(item);
		args.Resolution.Should().Be(DeleteArgs.DeleteResolutions.Delete);
	}
}
