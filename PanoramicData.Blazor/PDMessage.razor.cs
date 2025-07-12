namespace PanoramicData.Blazor;

public partial class PDMessage
{
	[Parameter] public ChatMessage Message { get; set; } = default!;
	[Parameter] public Func<ChatMessage, string?>? UserIconSelector { get; set; }
	[Parameter] public bool UseFullWidthMessages { get; set; } = true;
	[Parameter] public UserInfoMode UserInfoMode { get; set; } = UserInfoMode.UserOnlyOnRightOthersOnLeft;

	private bool ShouldShowMetaOnRight() => UserInfoMode switch
	{
		UserInfoMode.UserOnlyOnRightOthersOnLeft => Message.Sender.IsUser,
		UserInfoMode.UserOnlyOnLeftOthersOnRight => !Message.Sender.IsUser,
		UserInfoMode.AlwaysOnLeft => false,
		UserInfoMode.AlwaysOnRight => true,
		_ => Message.Sender.IsUser // Default fallback
	};

	private string GetMetaPositionClass()
		=> ShouldShowMetaOnRight() ? "meta-on-right" : "meta-on-left";
}