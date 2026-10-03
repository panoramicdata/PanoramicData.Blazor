using System.Globalization;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Metadata placement tests for <see cref="PDMessage"/>.
/// </summary>
public partial class PDMessageTests
{
	/// <summary>
	/// Verifies where each display mode puts the metadata for user and other senders, in the class and in
	/// the order of the parts.
	/// </summary>
	[Theory]
	[InlineData(MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft, true, true)]
	[InlineData(MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft, false, false)]
	[InlineData(MessageMetadataDisplayMode.UserOnlyOnLeftOthersOnRight, true, false)]
	[InlineData(MessageMetadataDisplayMode.UserOnlyOnLeftOthersOnRight, false, true)]
	[InlineData(MessageMetadataDisplayMode.AlwaysOnLeft, true, false)]
	[InlineData(MessageMetadataDisplayMode.AlwaysOnRight, false, true)]
	[InlineData((MessageMetadataDisplayMode)99, true, true)]
	public void DisplayMode_PlacesTheMetadata(MessageMetadataDisplayMode mode, bool fromUser, bool onRight)
	{
		var component = RenderMessage(Message(fromUser), p => p.Add(x => x.MessageMetadataDisplayMode, mode));

		var root = component.Find("div.pdchat-message");
		root.ClassList.Should().Contain([onRight ? "meta-on-right" : "meta-on-left", fromUser ? "user" : "bot", "full-width"]);
		TopLevelParts(component).Should().Equal(onRight ? ["pdchat-content", "pdchat-meta"] : ["pdchat-meta", "pdchat-content"]);
	}

	/// <summary>
	/// Verifies that the metadata shows the icon, sender name and formatted local timestamp.
	/// </summary>
	[Fact]
	public void Metadata_ShowsIconNameAndTimestamp()
	{
		var component = RenderMessage(Message(false), p => p
			.Add(x => x.UserIconSelector, m => m.Sender.Name == "Bot" ? "B" : null)
			.Add(x => x.MessageTimestampFormat, "HH:mm"));

		component.Find(".pdchat-icon").TextContent.Should().Be("B");
		component.Find(".pdchat-username").TextContent.Should().Be("Bot");
		component.Find(".pdchat-timestamp").TextContent.Should().Be(Sent.ToLocalTime().ToString("HH:mm", CultureInfo.CurrentCulture));
	}

	/// <summary>
	/// Verifies that with every piece of metadata switched off, no metadata area is rendered in either layout.
	/// </summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, true)]
	public void MetadataOff_RendersNoMetadataArea(bool fullWidth, bool fromUser)
	{
		var component = RenderMessage(Message(fromUser), p => p
			.Add(x => x.UseFullWidthMessages, fullWidth)
			.Add(x => x.ShowMessageUserIcon, false)
			.Add(x => x.ShowMessageUserName, false)
			.Add(x => x.ShowMessageTimestamp, false));

		component.FindAll(".pdchat-meta, .pdchat-bubble-header").Should().BeEmpty();
		component.Find(".pdchat-text").TextContent.Should().Contain("Hello there");
	}

	/// <summary>
	/// Verifies that individual metadata pieces can be switched off in each layout, and the default icon is used
	/// when no selector gives one.
	/// </summary>
	[Theory]
	[InlineData(true, true)]
	[InlineData(true, false)]
	[InlineData(false, false)]
	public void PartialMetadata_ShowsOnlyWhatIsSwitchedOn(bool fullWidth, bool fromUser)
	{
		var component = RenderMessage(Message(fromUser), p => p
			.Add(x => x.UseFullWidthMessages, fullWidth)
			.Add(x => x.ShowMessageUserName, false)
			.Add(x => x.ShowMessageTimestamp, false));

		component.FindAll(".pdchat-username, .pdchat-timestamp").Should().BeEmpty();
		component.Find(".pdchat-icon").TextContent.Should().Be("\U0001F464");
	}
}
