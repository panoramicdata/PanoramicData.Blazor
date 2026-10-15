namespace PanoramicData.Blazor.Demo.Pages;

public partial class PDCalendarDocumentation
{
	private static readonly DateOnly _date = new(2026, 10, 15);

	private readonly Meeting[] _meetings =
	[
		new("Planning", At(12, 9), At(12, 10)),
		new("Design review", At(14, 14), At(14, 15, 30)),
		new("Conference", At(20, 9), At(22, 17)),
		new("Retrospective", At(23, 16), At(23, 17))
	];

	/// <summary>
	/// An example item type: anything with a start and an end will do.
	/// </summary>
	public sealed record Meeting(string Title, DateTimeOffset Start, DateTimeOffset End);

	private static DateTimeOffset At(int day, int hour, int minute = 0) => new(2026, 10, day, hour, minute, 0, TimeSpan.FromHours(1));

	private const string _basicCode = """
		<PDCalendar TItem="Meeting"
		            Items="_meetings"
		            StartSelector="m => m.Start"
		            EndSelector="m => m.End"
		            TitleSelector="m => m.Title"
		            @bind-Date="_date"
		            @bind-View="_view" />

		@code {
		    private DateOnly? _date = new(2026, 10, 15);
		    private CalendarView _view = CalendarView.Month;

		    public sealed record Meeting(string Title, DateTimeOffset Start, DateTimeOffset End);

		    private readonly Meeting[] _meetings = [ /* ... */ ];
		}
		""";

	private const string _lazyCode = """
		<PDCalendar TItem="Broadcast"
		            Items="_items"
		            StartSelector="b => b.Start"
		            EndSelector="b => b.End"
		            TitleSelector="b => b.Title"
		            TimeZone="_london"
		            IsLoading="_isLoading"
		            OnVisibleRangeChanged="LoadAsync" />

		@code {
		    private IReadOnlyList<Broadcast> _items = [];
		    private bool _isLoading;

		    private async Task LoadAsync((DateTimeOffset From, DateTimeOffset To) range)
		    {
		        _isLoading = true;
		        _items = await Schedule.GetAsync(range.From, range.To);
		        _isLoading = false;
		    }
		}
		""";
}
