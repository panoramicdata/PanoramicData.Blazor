using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="MoveCopyArgs"/>.</summary>
public class MoveCopyArgsTests
{
	/// <summary>A new instance is an empty move that skips conflicts.</summary>
	[Fact]
	public void New_IsEmptyMoveThatSkipsConflicts()
	{
		var args = new MoveCopyArgs();

		args.Payload.Should().BeEmpty();
		args.TargetPath.Should().BeEmpty();
		args.TargetItems.Should().BeEmpty();
		args.Conflicts.Should().BeEmpty();
		args.IsCopy.Should().BeFalse();
		args.ConflictResolution.Should().Be(ConflictResolutions.Skip);
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var item = new FileExplorerItem { Path = "/a.txt" };

		var args = new MoveCopyArgs
		{
			Payload = [item],
			TargetPath = "/dest",
			TargetItems = [item],
			IsCopy = true,
			Conflicts = [item],
			ConflictResolution = ConflictResolutions.Overwrite
		};

		args.Payload.Should().ContainSingle();
		args.TargetPath.Should().Be("/dest");
		args.TargetItems.Should().ContainSingle();
		args.IsCopy.Should().BeTrue();
		args.Conflicts.Should().ContainSingle();
		args.ConflictResolution.Should().Be(ConflictResolutions.Overwrite);
	}
}
