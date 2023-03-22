namespace PanoramicData.Blazor.Models;

public record FieldDisplayOptions
{
	public string CssClass { get; init; } = string.Empty;

	public int WidthWeight { get; init; } = 1;
}

public record FieldBooleanOptions : FieldDisplayOptions
{
	public enum DisplayComponent
	{
		Checkbox,
		ToggleSwitch
	}

	public DisplayComponent Style { get; init; }

	public bool LabelBefore { get; init; }

	public bool Rounded { get; init; } = true;

	public string OnText { get; init; } = string.Empty;

	public string OffText { get; init; } = string.Empty;
}
