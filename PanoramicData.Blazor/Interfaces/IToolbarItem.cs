namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// The IToolbarItem interface defines properties and methods required by any Toolbar Item.
/// </summary>
public interface IToolbarItem
{
	/// <summary>
	/// Gets or sets the unique identifier for the toolbar item.
	/// </summary>
	string Key { get; set; }


	/// <summary>
	/// Gets or sets the tooltip for the toolbar item.
	/// </summary>
	string ToolTip { get; set; }

	/// <summary>
	/// Gets or sets whether the toolbar item is visible.
	/// </summary>
	bool IsVisible { get; set; }

	/// <summary>
	/// Gets or sets whether the toolbar item is enabled.
	/// </summary>
	bool IsEnabled { get; set; }

	/// <summary>
	/// Gets or sets whether the toolbar item is positioned further to the right of the previous toolbar item.
	/// </summary>
	bool ShiftRight { get; set; }
}
