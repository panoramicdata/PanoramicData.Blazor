namespace PanoramicData.Blazor.PDLogData;
internal class LogEntry
{
	public required LogLevel LogLevel { get; init; }
	public required string Message { get; init; }
	public required DateTime Timestamp { get; init; }
	public required string Icon { get; init; }
	public required string TimestampClass { get; init; }
	public required Exception? Exception { get; init; }
}
