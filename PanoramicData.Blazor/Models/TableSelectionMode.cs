namespace PanoramicData.Blazor.Models;

public enum TableSelectionMode
{
	None,
#pragma warning disable CA1720 // 'Single' is the correct domain term for this selection mode
	Single,
#pragma warning restore CA1720
	Multiple
}
