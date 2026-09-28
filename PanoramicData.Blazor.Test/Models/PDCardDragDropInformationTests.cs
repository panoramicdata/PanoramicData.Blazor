using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="PDCardDragDropInformation"/>.</summary>
public class PDCardDragDropInformationTests
{
	/// <summary>A new instance is not dragging and targets index zero.</summary>
	[Fact]
	public void New_IsNotDragging()
	{
		var info = new PDCardDragDropInformation();

		info.IsDragging.Should().BeFalse();
		info.TargetIndex.Should().Be(0);
	}

	/// <summary>Reset stops the drag and clears the target to minus one, meaning no target.</summary>
	[Fact]
	public void Reset_ClearsDragAndTarget()
	{
		var info = new PDCardDragDropInformation { IsDragging = true, TargetIndex = 4 };

		info.Reset();

		info.IsDragging.Should().BeFalse();
		info.TargetIndex.Should().Be(-1);
	}
}
