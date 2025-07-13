namespace PanoramicData.Blazor;
public partial class PDChatContainer
{
	[Parameter] public RenderFragment? ChildContent { get; set; }
	[Parameter] public RenderFragment? ChatContent { get; set; }
	[Parameter] public PDChatDockMode DockMode { get; set; } = PDChatDockMode.BottomRight;
	[Parameter] public int GutterSize { get; set; } = 6;
	[Parameter] public int ChatPanelSize { get; set; } = 2;
	[Parameter] public int TotalSize { get; set; } = 5;
	[Parameter] public int ChatMinSize { get; set; } = 200;
	[Parameter] public int ContentMinSize { get; set; } = 200;

	private bool IsSplitMode => DockMode is PDChatDockMode.Left or PDChatDockMode.Right;

	private SplitDirection GetSplitDirection()
	{
		return DockMode switch
		{
			PDChatDockMode.Left or PDChatDockMode.Right => SplitDirection.Horizontal,
			_ => SplitDirection.Horizontal
		};
	}

	private bool IsChatFirstPanel()
	{
		return DockMode switch
		{
			PDChatDockMode.Left => true,
			PDChatDockMode.Right => false,
			_ => false
		};
	}
}