namespace PanoramicData.Blazor.Models.ColorPicker;

/// <summary>
/// Represents a named color in a palette.
/// </summary>
public class PaletteColor
{
	/// <summary>
	/// Gets or sets the color value.
	/// </summary>
	public string Color { get; set; } = "#000000";

	/// <summary>
	/// Gets or sets the display name of the color.
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// Gets or sets optional tooltip text.
	/// </summary>
	public string? ToolTip { get; set; }

	/// <summary>
	/// Creates a new PaletteColor with default values.
	/// </summary>
	public PaletteColor()
	{
	}

	/// <summary>
	/// Creates a new PaletteColor with the specified color.
	/// </summary>
	public PaletteColor(string color, string? name = null)
	{
		Color = color;
		Name = name;
	}
}

/// <summary>
/// Predefined color palettes.
/// </summary>
public static class ColorPalettes
{
	/// <summary>
	/// Basic web colors palette.
	/// </summary>
	public static List<PaletteColor> Basic =>
	[
		new("#FFFFFF", "White"),
		new("#C0C0C0", "Silver"),
		new("#808080", "Gray"),
		new("#000000", "Black"),
		new("#FF0000", "Red"),
		new("#800000", "Maroon"),
		new("#FFFF00", "Yellow"),
		new("#808000", "Olive"),
		new("#00FF00", "Lime"),
		new("#008000", "Green"),
		new("#00FFFF", "Aqua"),
		new("#008080", "Teal"),
		new("#0000FF", "Blue"),
		new("#000080", "Navy"),
		new("#FF00FF", "Fuchsia"),
		new("#800080", "Purple")
	];

	/// <summary>
	/// Material Design primary colors palette.
	/// </summary>
	public static List<PaletteColor> Material =>
	[
		new("#F44336", "Red"),
		new("#E91E63", "Pink"),
		new("#9C27B0", "Purple"),
		new("#673AB7", "Deep Purple"),
		new("#3F51B5", "Indigo"),
		new("#2196F3", "Blue"),
		new("#03A9F4", "Light Blue"),
		new("#00BCD4", "Cyan"),
		new("#009688", "Teal"),
		new("#4CAF50", "Green"),
		new("#8BC34A", "Light Green"),
		new("#CDDC39", "Lime"),
		new("#FFEB3B", "Yellow"),
		new("#FFC107", "Amber"),
		new("#FF9800", "Orange"),
		new("#FF5722", "Deep Orange"),
		new("#795548", "Brown"),
		new("#9E9E9E", "Grey"),
		new("#607D8B", "Blue Grey")
	];

	/// <summary>
	/// Extended web colors palette.
	/// </summary>
	public static List<PaletteColor> Extended =>
	[
		// Reds
		new("#FF0000", "Red"),
		new("#DC143C", "Crimson"),
		new("#B22222", "Firebrick"),
		new("#8B0000", "Dark Red"),
		// Oranges
		new("#FF8C00", "Dark Orange"),
		new("#FF7F50", "Coral"),
		new("#FF6347", "Tomato"),
		new("#FF4500", "Orange Red"),
		// Yellows
		new("#FFD700", "Gold"),
		new("#FFFF00", "Yellow"),
		new("#F0E68C", "Khaki"),
		new("#BDB76B", "Dark Khaki"),
		// Greens
		new("#ADFF2F", "Green Yellow"),
		new("#00FF00", "Lime"),
		new("#32CD32", "Lime Green"),
		new("#228B22", "Forest Green"),
		new("#006400", "Dark Green"),
		new("#2E8B57", "Sea Green"),
		// Cyans
		new("#00FFFF", "Cyan"),
		new("#20B2AA", "Light Sea Green"),
		new("#008B8B", "Dark Cyan"),
		new("#40E0D0", "Turquoise"),
		// Blues
		new("#1E90FF", "Dodger Blue"),
		new("#0000FF", "Blue"),
		new("#0000CD", "Medium Blue"),
		new("#00008B", "Dark Blue"),
		new("#191970", "Midnight Blue"),
		new("#4169E1", "Royal Blue"),
		// Purples
		new("#8A2BE2", "Blue Violet"),
		new("#9932CC", "Dark Orchid"),
		new("#9400D3", "Dark Violet"),
		new("#800080", "Purple"),
		new("#4B0082", "Indigo"),
		// Pinks
		new("#FF1493", "Deep Pink"),
		new("#FF69B4", "Hot Pink"),
		new("#FFB6C1", "Light Pink"),
		new("#FFC0CB", "Pink"),
		// Grays
		new("#FFFFFF", "White"),
		new("#DCDCDC", "Gainsboro"),
		new("#C0C0C0", "Silver"),
		new("#A9A9A9", "Dark Gray"),
		new("#808080", "Gray"),
		new("#696969", "Dim Gray"),
		new("#000000", "Black")
	];

	/// <summary>
	/// Grayscale palette.
	/// </summary>
	public static List<PaletteColor> Grayscale =>
	[
		new("#FFFFFF", "White"),
		new("#F5F5F5", "95%"),
		new("#EBEBEB", "90%"),
		new("#E0E0E0", "85%"),
		new("#D6D6D6", "80%"),
		new("#CCCCCC", "75%"),
		new("#C2C2C2", "70%"),
		new("#B8B8B8", "65%"),
		new("#ADADAD", "60%"),
		new("#A3A3A3", "55%"),
		new("#999999", "50%"),
		new("#8F8F8F", "45%"),
		new("#858585", "40%"),
		new("#7A7A7A", "35%"),
		new("#707070", "30%"),
		new("#666666", "25%"),
		new("#5C5C5C", "20%"),
		new("#525252", "15%"),
		new("#474747", "10%"),
		new("#3D3D3D", "5%"),
		new("#000000", "Black")
	];
}
