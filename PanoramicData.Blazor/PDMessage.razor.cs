namespace PanoramicData.Blazor;
public partial class PDMessage
{
	[Parameter] public ChatMessage Message { get; set; } = default!;
	[Parameter] public Func<ChatMessage, string?>? UserIconSelector { get; set; }
}