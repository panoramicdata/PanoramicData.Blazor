using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// A logger that records every entry written to it, so a test can assert that a component reported a
/// problem rather than swallowing it.
/// </summary>
/// <typeparam name="T">The logger category.</typeparam>
internal sealed class ListLogger<T> : ILogger<T>
{
	/// <summary>Gets the entries written so far, in order.</summary>
	public List<ListLoggerEntry> Entries { get; } = [];

	/// <inheritdoc />
	public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		=> NullLogger.Instance.BeginScope(state);

	/// <inheritdoc />
	public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

	/// <inheritdoc />
	public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		=> Entries.Add(new ListLoggerEntry(logLevel, eventId, formatter(state, exception), exception));
}

/// <summary>
/// One entry recorded by a <see cref="ListLogger{T}"/>.
/// </summary>
/// <param name="Level">The level it was logged at.</param>
/// <param name="EventId">The event id it was logged with.</param>
/// <param name="Message">The formatted message.</param>
/// <param name="Exception">The exception logged with it, if any.</param>
internal sealed record ListLoggerEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);
