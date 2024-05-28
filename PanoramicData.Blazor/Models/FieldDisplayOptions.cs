namespace PanoramicData.Blazor.Models;

public record FieldDisplayOptions
{
	public bool AllowNulls { get; set; }

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

public record FieldDateTimeOptions : FieldDisplayOptions
{
	public bool ShowOffset { get; init; }

	public bool ShowTime { get; init; }

	public int TimeStepSecs { get; init; } = 1;
}

public record FieldStringOptions : FieldDisplayOptions
{
	public enum Editors
	{
		TextBox,
		TextArea,
		Monaco
	}

	public FieldStringOptions()
	{
		CssClass = "h-100";
	}

	public string CodeLanguage { get; init; } = string.Empty;

	public Editors Editor { get; init; }

	public bool Resize { get; init; }

	public int Rows { get; init; } = 4;

	public Func<StandaloneCodeEditor, StandaloneEditorConstructionOptions> MonacoOptions { get; init; } = (_)
		=> new StandaloneEditorConstructionOptions { ReadOnly = true };
}