namespace PanoramicData.Blazor.Models;

/// <summary>
/// Base display options shared by all field types.
/// </summary>
public record FieldDisplayOptions
{
	/// <summary>Gets or sets whether the field accepts null values.</summary>
	public bool AllowNulls { get; set; }

	/// <summary>Gets or sets an additional CSS class applied to the field element.</summary>
	public string CssClass { get; init; } = string.Empty;

	/// <summary>Gets or sets a relative weight used when distributing field widths in a layout grid.</summary>
	public int WidthWeight { get; init; } = 1;
}

/// <summary>
/// Display options for boolean fields.
/// </summary>
public record FieldBooleanOptions : FieldDisplayOptions
{
	/// <summary>
	/// Specifies the UI control used to render a boolean field.
	/// </summary>
	public enum DisplayComponent
	{
		/// <summary>Renders as a standard checkbox.</summary>
		Checkbox,
		/// <summary>Renders as a toggle switch.</summary>
		ToggleSwitch
	}

	/// <summary>Gets or sets the UI control style used to render the boolean field.</summary>
	public DisplayComponent Style { get; init; }

	/// <summary>Gets or sets whether the label is rendered before (to the left of) the control.</summary>
	public bool LabelBefore { get; init; }

	/// <summary>Gets or sets whether toggle switch corners are rounded.</summary>
	public bool Rounded { get; init; } = true;

	/// <summary>Gets or sets the text shown on the switch when the value is true.</summary>
	public string OnText { get; init; } = string.Empty;

	/// <summary>Gets or sets the text shown on the switch when the value is false.</summary>
	public string OffText { get; init; } = string.Empty;
}

/// <summary>
/// Display options for date/time fields.
/// </summary>
public record FieldDateTimeOptions : FieldDisplayOptions
{
	/// <summary>Gets or sets whether the UTC offset is displayed alongside the date/time value.</summary>
	public bool ShowOffset { get; init; }

	/// <summary>Gets or sets whether the time portion is shown in the date/time editor.</summary>
	public bool ShowTime { get; init; }

	/// <summary>Gets or sets the step in seconds used by the time input spinner.</summary>
	public int TimeStepSecs { get; init; } = 1;
}

/// <summary>
/// Display options for string fields.
/// </summary>
public record FieldStringOptions : FieldDisplayOptions
{
	/// <summary>
	/// Specifies the editor control used to render a string field.
	/// </summary>
	public enum Editors
	{
		/// <summary>Renders as a single-line text box.</summary>
		TextBox,
		/// <summary>Renders as a multi-line text area.</summary>
		TextArea,
		/// <summary>Renders as a Monaco code editor.</summary>
		Monaco
	}

	/// <summary>
	/// Initializes a new instance of <see cref="FieldStringOptions"/>.
	/// </summary>
	public FieldStringOptions()
	{
		CssClass = "";
	}

	/// <summary>Gets or sets the Monaco editor language identifier (e.g. "json", "csharp").</summary>
	public string CodeLanguage { get; init; } = string.Empty;

	/// <summary>Gets or sets the editor control used to render the string field.</summary>
	public Editors Editor { get; init; }

	/// <summary>Gets or sets whether the text area can be resized by the user.</summary>
	public bool Resize { get; init; }

	/// <summary>Gets or sets an additional CSS class applied when the text area is in resize mode.</summary>
	public string ResizeCssCls { get; init; } = string.Empty;

	/// <summary>Gets or sets the number of visible rows for a text area editor.</summary>
	public int Rows { get; init; } = 4;

	/// <summary>Gets or sets a factory function that supplies construction options for the Monaco editor.</summary>
	public Func<StandaloneCodeEditor, StandaloneEditorConstructionOptions> MonacoOptions { get; init; } = (_)
		=> new StandaloneEditorConstructionOptions { ReadOnly = true };
}