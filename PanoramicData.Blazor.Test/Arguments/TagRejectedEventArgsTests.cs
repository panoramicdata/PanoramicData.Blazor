using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Arguments;

/// <summary>Tests for <see cref="TagRejectedEventArgs"/>.</summary>
public class TagRejectedEventArgsTests
{
	/// <summary>The constructor captures the rejected tag and the reason for each reason value.</summary>
	[Theory]
	[InlineData(TagRejectionReason.Duplicate)]
	[InlineData(TagRejectionReason.TooLong)]
	[InlineData(TagRejectionReason.MaxTagsReached)]
	[InlineData(TagRejectionReason.NotInSuggestions)]
	public void Constructor_CapturesTagAndReason(TagRejectionReason reason)
	{
		var args = new TagRejectedEventArgs("urgent", reason);

		args.Tag.Should().Be("urgent");
		args.Reason.Should().Be(reason);
		args.Should().BeAssignableTo<EventArgs>();
	}
}
