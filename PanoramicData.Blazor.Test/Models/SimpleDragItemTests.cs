using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="SimpleDragItem"/>, <see cref="JobModel"/> and <see cref="JobStatuses"/>.</summary>
public class SimpleDragItemTests
{
	/// <summary>A new drag item can be dragged, has no text and a unique guid identifier.</summary>
	[Fact]
	public void New_IsDraggableWithUniqueId()
	{
		var a = new SimpleDragItem();
		var b = new SimpleDragItem();

		a.CanDrag.Should().BeTrue();
		a.Text.Should().BeEmpty();
		Guid.TryParse(a.Id, out _).Should().BeTrue();
		a.Id.Should().NotBe(b.Id);
	}

	/// <summary>The item describes itself by its text, and its members round-trip.</summary>
	[Fact]
	public void ToString_ReturnsText()
	{
		var item = new SimpleDragItem { Text = "Card 1", Id = "c1", CanDrag = false };

		item.ToString().Should().Be("Card 1");
		item.Id.Should().Be("c1");
		item.CanDrag.Should().BeFalse();
	}

	/// <summary>A job model starts as a to-do item, and its members round-trip.</summary>
	[Fact]
	public void JobModel_DefaultsAndRoundTrip()
	{
		var updated = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var job = new JobModel();
		job.Status.Should().Be(JobStatuses.Todo);
		job.Description.Should().BeEmpty();

		job.Id = 5;
		job.Status = JobStatuses.Completed;
		job.Description = "Ship it";
		job.LastUpdated = updated;

		job.Id.Should().Be(5);
		job.Status.Should().Be(JobStatuses.Completed);
		job.Description.Should().Be("Ship it");
		job.LastUpdated.Should().Be(updated);
	}
}
