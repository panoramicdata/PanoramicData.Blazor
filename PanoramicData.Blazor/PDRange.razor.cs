namespace PanoramicData.Blazor;

public partial class PDRange
{
	private ElementReference _svgPlotElement;
	private const double HandleWidth = 10;

	[Parameter]
	public double Height { get; set; } = 20;

	[Parameter]
	public bool Invert { get; set; }

	[Parameter]
	public RangeOptions Options { get; set; } = new();

	[Parameter]
	public NumericRange Range { get; set; } = new();

	[Parameter]
	public double Max { get; set; } = default;

	[Parameter]
	public double Min { get; set; } = default;

	[Parameter]
	[Required]
	public string? Message { get; set; }

	[Parameter]
	public EventCallback<NumericRange> RangeChanged { get; set; }

	[Parameter]
	public double Step { get; set; } = default;

	[Parameter]
	public double Width { get; set; } = 400;

	#region Common Expressions

	private double HalfHeight => Height / 2;

	private double QuarterHeight => Height / 4;

	private double StartHandleX => 1 + Math.Round((Range.Start / Max) * TrackWidth, 2);

	private double EndHandleX => 1 + Math.Round((Range.End / Max) * TrackWidth, 2);

	private double TrackWidth => Width - HandleWidth - 2;

	#endregion

	protected override void Validate()
	{
		base.Validate();

		if (Min > Max)
		{
			ParameterValidationErrors.Add("Min", "Value must be lower than Max");
		}

		if (Range.Start < Min)
		{
			ParameterValidationErrors.Add("Range.Start", "Must be equal or higher than Min");
		}
		else if (Range.Start > Range.End)
		{
			ParameterValidationErrors.Add("Range.Start", "Must be equal or lower than Range.End");
		}

		if (Range.End > Max)
		{
			ParameterValidationErrors.Add("Range.End", "Must be equal or lower than Max");
		}
		else if (Range.End < Range.Start)
		{
			ParameterValidationErrors.Add("Range.End", "Must be equal or higher than Range.Start");
		}
	}
}
