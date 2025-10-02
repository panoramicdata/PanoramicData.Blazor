namespace PanoramicData.Blazor;

public partial class PDMessage
{
	/// <summary>
	/// Gets or sets the chat message to display.
	/// </summary>
	[Parameter] public ChatMessage Message { get; set; } = default!;

	/// <summary>
	/// A function to select a user icon for a given message.
	/// </summary>
	[Parameter] public Func<ChatMessage, string?>? UserIconSelector { get; set; }

	/// <summary>
	/// Gets or sets whether messages should use the full width of the container.
	/// </summary>
	[Parameter] public bool UseFullWidthMessages { get; set; } = true;

	/// <summary>
	/// Gets or sets how message metadata is displayed.
	/// </summary>
	[Parameter] public MessageMetadataDisplayMode MessageMetadataDisplayMode { get; set; } = MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft;

	/// <summary>
	/// Gets or sets whether to show the user icon for each message.
	/// </summary>
	[Parameter] public bool ShowMessageUserIcon { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the user name for each message.
	/// </summary>
	[Parameter] public bool ShowMessageUserName { get; set; } = true;

	/// <summary>
	/// Gets or sets whether to show the timestamp for each message.
	/// </summary>
	[Parameter] public bool ShowMessageTimestamp { get; set; } = true;

	/// <summary>
	/// Gets or sets the format for the message timestamp.
	/// </summary>
	[Parameter] public string MessageTimestampFormat { get; set; } = "HH:mm:ss";

	private bool ShouldShowMetaOnRight() => MessageMetadataDisplayMode switch
	{
		MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft => Message.Sender.IsUser,
		MessageMetadataDisplayMode.UserOnlyOnLeftOthersOnRight => !Message.Sender.IsUser,
		MessageMetadataDisplayMode.AlwaysOnLeft => false,
		MessageMetadataDisplayMode.AlwaysOnRight => true,
		_ => Message.Sender.IsUser // Default fallback
	};

	private string GetMetaPositionClass()
		=> ShouldShowMetaOnRight() ? "meta-on-right" : "meta-on-left";

	private bool ShouldShowAnyMetadata()
		=> ShowMessageUserIcon || ShowMessageUserName || ShowMessageTimestamp;

	private string GetFormattedTimestamp()
		=> Message.Timestamp.ToLocalTime().ToString(MessageTimestampFormat);
}