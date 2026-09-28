using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="DownloadResponse"/>.</summary>
public class DownloadResponseTests
{
	/// <summary>A new response is an unsuccessful, empty download.</summary>
	[Fact]
	public void New_IsUnsuccessfulAndEmpty()
	{
		var response = new DownloadResponse();

		response.Success.Should().BeFalse();
		response.ErrorMessage.Should().BeEmpty();
		response.FileName.Should().BeEmpty();
		response.Content.Should().BeEmpty();
	}

	/// <summary>All members round-trip.</summary>
	[Fact]
	public void SettableMembers_RoundTrip()
	{
		var response = new DownloadResponse
		{
			Success = true,
			ErrorMessage = "none",
			FileName = "report.pdf",
			Content = [1, 2, 3]
		};

		response.Success.Should().BeTrue();
		response.ErrorMessage.Should().Be("none");
		response.FileName.Should().Be("report.pdf");
		response.Content.Should().Equal(1, 2, 3);
	}
}
