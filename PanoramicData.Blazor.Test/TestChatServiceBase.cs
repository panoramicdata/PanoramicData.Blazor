using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Boilerplate for an <see cref="IChatService"/> test double: implements the presentation and configuration
/// members that every implementation is obliged to supply, leaving a derived double to say only what its test
/// is about.
/// </summary>
/// <remarks>
/// <para>
/// This deliberately implements <b>only the members <see cref="IChatService"/> requires</b>. It does not touch
/// any member that carries a default interface implementation - not the toast API and not the
/// conversation-addressed members - because a double that supplied its own version of those would make tests
/// pass by agreeing with themselves rather than by exercising the interface.
/// </para>
/// <para>
/// Test-only, and so it lives in the test project rather than beside the production services.
/// </para>
/// </remarks>
public abstract class TestChatServiceBase : IChatService
{
	/// <inheritdoc />
	public bool IsLive { get; set; } = true;

	/// <inheritdoc />
	public PDChatDockMode DockMode { get; set; } = PDChatDockMode.BottomRight;

	/// <inheritdoc />
	public PDChatDockMode PreferredDockMode { get; set; } = PDChatDockMode.BottomRight;

	/// <inheritdoc />
	public PDChatDockMode RestoreMode { get; set; } = PDChatDockMode.BottomRight;

	/// <inheritdoc />
	public PDChatButtonPosition MinimizedButtonPosition { get; set; } = PDChatButtonPosition.BottomRight;

	/// <inheritdoc />
	public bool IsMuted { get; set; }

	/// <inheritdoc />
	public string Title { get; set; } = "Test Chat";

	/// <inheritdoc />
	public bool IsMaximizePermitted { get; set; } = true;

	/// <inheritdoc />
	public bool IsCanvasUsePermitted { get; set; } = true;

	/// <inheritdoc />
	public bool IsClearPermitted { get; set; } = true;

	/// <inheritdoc />
	public bool AutoRestoreOnNewMessage { get; set; }

	/// <inheritdoc />
	public bool UseFullWidthMessages { get; set; } = true;

	/// <inheritdoc />
	public MessageMetadataDisplayMode MessageMetadataDisplayMode { get; set; }
		= MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft;

	/// <inheritdoc />
	public bool ShowMessageUserIcon { get; set; } = true;

	/// <inheritdoc />
	public bool ShowMessageUserName { get; set; } = true;

	/// <inheritdoc />
	public bool ShowMessageTimestamp { get; set; } = true;

	/// <inheritdoc />
	public string MessageTimestampFormat { get; set; } = "HH:mm:ss";

	/// <inheritdoc />
	[Obsolete("Required by the interface for backward compatibility; superseded by the toast API.")]
	public bool ShowLastMessage { get; set; } = true;

	/// <inheritdoc />
	[Obsolete("Required by the interface for backward compatibility; superseded by the toast API.")]
	public double ShowLastMessageDurationSeconds { get; set; } = 5d;

	/// <inheritdoc />
	public abstract IReadOnlyList<ChatMessage> Messages { get; }

	/// <inheritdoc />
	public event Action<ChatMessage>? OnMessageReceived;

	/// <inheritdoc />
	public event Action<bool>? OnLiveStatusChanged;

	/// <inheritdoc />
	public event Action<PDChatDockMode>? OnDockModeChanged;

	/// <inheritdoc />
	public event Action<bool>? OnMuteStatusChanged;

	/// <inheritdoc />
	public event Action? OnConfigurationChanged;

	/// <inheritdoc />
	public abstract void SendMessage(ChatMessage chatMessage);

	/// <inheritdoc />
	public abstract void ClearMessages();

	/// <inheritdoc />
	public void Initialize()
	{
	}

	/// <inheritdoc />
	public void Dispose() => GC.SuppressFinalize(this);

	/// <summary>
	/// Raises <see cref="OnMessageReceived"/>. Present so that the events above are not merely declared and
	/// never used, which the compiler would otherwise warn about, and so a double can simulate an inbound
	/// message on the singular path.
	/// </summary>
	protected void RaiseMessageReceived(ChatMessage message)
	{
		OnMessageReceived?.Invoke(message);
		OnLiveStatusChanged?.Invoke(IsLive);
		OnDockModeChanged?.Invoke(DockMode);
		OnMuteStatusChanged?.Invoke(IsMuted);
		OnConfigurationChanged?.Invoke();
	}
}
