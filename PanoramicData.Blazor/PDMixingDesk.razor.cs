namespace PanoramicData.Blazor;

/// <summary>
/// A Blazor component that provides a mixing-desk style container for audio channel controls.
/// </summary>
public partial class PDMixingDesk
{
	/// <summary>
	/// Gets or sets the child content (typically PDAudioChannel components).
	/// </summary>
	[Parameter]
	public RenderFragment? ChildContent { get; set; }

	/// <summary>
	/// Gets or sets additional CSS classes.
	/// </summary>
	[Parameter]
	public string CssClass { get; set; } = string.Empty;

	// Set once the consumer supplies MinHeight, so the documented default is reported but never applied
	private bool _isMinHeightSet;

	/// <summary>
	/// Gets or sets the minimum height of the mixing desk, as a CSS length such as "600px".
	/// </summary>
	/// <remarks>
	/// The value is applied as the container's min-height only when it is set explicitly. When it is left
	/// unset, the property reports the documented default of "600px" but no min-height is applied, so a desk
	/// that does not set it keeps its natural height, as it always has. Setting an empty value applies none.
	/// </remarks>
	[Parameter]
	public string MinHeight { get; set; } = "600px";

	/// <summary>
	/// Gets the inline style for the container, or null (no style attribute) when no minimum height has been
	/// set explicitly, or it was set empty.
	/// </summary>
	private string? Style => !_isMinHeightSet || string.IsNullOrWhiteSpace(MinHeight) ? null : $"min-height: {MinHeight}";

	/// <inheritdoc />
	public override Task SetParametersAsync(ParameterView parameters)
	{
		if (parameters.TryGetValue<string>(nameof(MinHeight), out _))
		{
			_isMinHeightSet = true;
		}

		return base.SetParametersAsync(parameters);
	}
}
