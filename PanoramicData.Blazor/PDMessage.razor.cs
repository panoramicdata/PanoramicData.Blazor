namespace PanoramicData.Blazor;

public partial class PDMessage
{
	[Parameter] public ChatMessage Message { get; set; } = default!;
	[Parameter] public Func<ChatMessage, string?>? UserIconSelector { get; set; }
	[Parameter] public bool UseFullWidthMessages { get; set; } = true;
	[Parameter] public MessageMetadataDisplayMode MessageMetadataDisplayMode { get; set; } = MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft;
	[Parameter] public bool ShowMessageUserIcon { get; set; } = true;
	[Parameter] public bool ShowMessageUserName { get; set; } = true;
	[Parameter] public bool ShowMessageTimestamp { get; set; } = true;
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