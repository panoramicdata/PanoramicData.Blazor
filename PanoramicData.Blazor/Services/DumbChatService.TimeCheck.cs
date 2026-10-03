using System;

namespace PanoramicData.Blazor.Services;

/// <summary>
/// The initialization of <see cref="DumbChatService"/>, which brings it online and starts the periodic time check.
/// </summary>
public partial class DumbChatService
{
	private bool _isInitialized;
	private bool _isOnline;
	private Timer? _timer;

	/// <summary>
	/// Gets the predefined sender used for periodic time-check messages.
	/// </summary>
	public static ChatMessageSender TimeBot { get; } = new()
	{
		Name = "TimeBot",
		IsUser = false,
		IsHuman = false,
		IsSupport = false
	};

	/// <inheritdoc />
	public void Initialize()
	{
		if (_isInitialized)
		{
			return;
		}

		// Start the timer to send a time check every minute
		_timer = new Timer(_ => SendTimeCheck(), null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5));
		_isInitialized = true;
		_isOnline = true;
	}

	/// <inheritdoc />
	public bool IsLive => _isInitialized && _isOnline;

	/// <summary>
	/// Delivers a time check from <see cref="TimeBot"/>; called by the periodic timer started in <see cref="Initialize"/>.
	/// </summary>
	internal void SendTimeCheck()
	{
		var message = new ChatMessage
		{
			Id = Guid.NewGuid(),
			Sender = TimeBot,
			Title = "Time Check",
			Message = $"The current time is {DateTime.Now:T}",
			Type = MessageType.Normal,
			Timestamp = DateTimeOffset.Now
		};

		// The periodic time check is ambient rather than a reply, so it lands in whatever conversation the
		// user is currently looking at.
		Deliver(_activeConversationId, message);
	}
}
