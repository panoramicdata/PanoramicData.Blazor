namespace PanoramicData.Blazor.Models;

/// <summary>
/// One row of the hour axis shared by the day columns of a week view.
/// </summary>
/// <param name="Hour">The local hour of the day, from 0 to 23.</param>
/// <param name="Occurrence">0 for the first time the hour occurs in a day, 1 for the repeated hour when the clocks go back.</param>
/// <param name="IsAmbiguous">True when some visible day sees this local hour more than once, so the label should carry its UTC offset.</param>
public sealed record CalendarAxisRow(int Hour, int Occurrence, bool IsAmbiguous);
