namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDRangePage
{
	protected bool IsEnabled { get; set; } = true;
	protected bool Invert { get; set; }
	protected NumericRange Range1 { get; set; } = new(25, 75);
	protected NumericRange Range2 { get; set; } = new(10, 18);
}
