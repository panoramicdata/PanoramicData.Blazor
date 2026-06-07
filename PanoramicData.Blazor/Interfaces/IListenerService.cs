namespace PanoramicData.Blazor.Interfaces;

/// <summary>
/// Provides listener state management and fan-out stream events.
/// </summary>
public interface IListenerService
{
	/// <summary>
	/// Event raised for each listener input item.
	/// </summary>
	event EventHandler<ListenerInput>? InputReceived;

	/// <summary>
	/// Event raised when listener state changes.
	/// </summary>
	event EventHandler<ListenerStatusChangedEventArgs>? StatusChanged;

	/// <summary>
	/// Gets the current listener mode.
	/// </summary>
	ListenerMode Mode { get; }

	/// <summary>
	/// Gets the current listener state.
	/// </summary>
	ListenerState State { get; }

	/// <summary>
	/// Applies listener configuration.
	/// </summary>
	/// <param name="configuration">Configuration to apply.</param>
	void Configure(ListenerConfiguration configuration);

	/// <summary>
	/// Starts listening in manual mode.
	/// </summary>
	void StartListening();

	/// <summary>
	/// Stops listening in manual mode.
	/// </summary>
	void StopListening();

	/// <summary>
	/// Handles recognized text from an input source.
	/// </summary>
	/// <param name="text">Recognized text.</param>
	/// <param name="timestamp">Recognition timestamp.</param>
	void HandleRecognizedText(string text, DateTimeOffset timestamp);

	/// <summary>
	/// Sets listener state to active listening.
	/// </summary>
	void HandleListeningStarted();

	/// <summary>
	/// Sets listener state to idle listening.
	/// </summary>
	void HandleListeningStopped();

	/// <summary>
	/// Marks listener as unsupported.
	/// </summary>
	void HandleUnsupported();

	/// <summary>
	/// Marks listener as permission denied.
	/// </summary>
	void HandlePermissionDenied();

	/// <summary>
	/// Marks listener as failed.
	/// </summary>
	/// <param name="errorCode">Error code from listener source.</param>
	/// <param name="message">Optional message.</param>
	void HandleError(string? errorCode, string? message);
}
