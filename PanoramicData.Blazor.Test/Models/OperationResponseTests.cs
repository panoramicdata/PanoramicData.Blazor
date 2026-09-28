using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="OperationResponse"/>.</summary>
public class OperationResponseTests
{
	/// <summary>A new response is unsuccessful with no message, and both members round-trip.</summary>
	[Fact]
	public void Members_DefaultAndRoundTrip()
	{
		var response = new OperationResponse();
		response.Success.Should().BeFalse();
		response.ErrorMessage.Should().BeEmpty();

		response.Success = true;
		response.ErrorMessage = "warning";

		response.Success.Should().BeTrue();
		response.ErrorMessage.Should().Be("warning");
	}
}
