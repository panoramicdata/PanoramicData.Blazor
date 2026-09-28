using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="UploadsReadyEventArgs"/>.</summary>
public class UploadsReadyEventArgsTests
{
	/// <summary>A new instance has no files and neither cancels nor overwrites.</summary>
	[Fact]
	public void New_HasNoFilesAndNoFlags()
	{
		var args = new UploadsReadyEventArgs();

		args.Cancel.Should().BeFalse();
		args.Overwrite.Should().BeFalse();
		args.Files.Should().BeEmpty();
		args.FilesToSkip.Should().BeEmpty();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var file = new DropZoneFile { Name = "a.txt" };

		var args = new UploadsReadyEventArgs
		{
			Cancel = true,
			Overwrite = true,
			Files = [file],
			FilesToSkip = [file]
		};

		args.Cancel.Should().BeTrue();
		args.Overwrite.Should().BeTrue();
		args.Files.Should().ContainSingle();
		args.FilesToSkip.Should().ContainSingle();
	}
}
