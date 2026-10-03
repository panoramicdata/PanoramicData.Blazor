using System;

namespace PanoramicData.Blazor.Services;

/// <summary>
/// The presentation and toast settings of <see cref="DumbChatService"/>, each announcing a change through
/// <see cref="OnConfigurationChanged"/> (or, for <see cref="IsMuted"/>, <see cref="OnMuteStatusChanged"/>).
/// </summary>
public partial class DumbChatService
{
	private bool _isMuted;
	private bool _isMaximizePermitted = true;
	private bool _isCanvasUsePermitted = true;
	private bool _isClearPermitted = true;
	private bool _isInputPermitted = true;
	private string? _inputDisabledMessage;
	private bool _autoRestoreOnNewMessage;
	private bool _useFullWidthMessages = true;
	private MessageMetadataDisplayMode _messageMetadataDisplayMode = MessageMetadataDisplayMode.UserOnlyOnRightOthersOnLeft;
	private bool _showMessageUserIcon = true;
	private bool _showMessageUserName = true;
	private bool _showMessageTimestamp = true;
	private string _messageTimestampFormat = "HH:mm:ss";
	private string _title = "Demo Chat";
	private PDChatDockMode _restoreMode = PDChatDockMode.BottomRight;
	private PDChatButtonPosition _minimizedButtonPosition = PDChatButtonPosition.BottomRight;
	private bool _toastEnabled = true;
	private double _toastDisplayDurationSeconds = 5.0;
	private PDChatToastAnimation _toastEntryAnimation = PDChatToastAnimation.Grow;
	private PDChatToastAnimation _toastExitAnimation = PDChatToastAnimation.Shrink;
	private double _toastAnimationDurationMs = 250d;
	private bool _toastAutoDismiss = true;
	private bool _toastShowTitle = true;
	private string _toastMinWidth = "200px";
	private string _toastMaxWidth = "300px";
	private string _toastMinHeight = string.Empty;
	private string _toastMaxHeight = string.Empty;
	private int _toastMaxVisible = 5;
	private PDChatButtonPosition _toastAnchor = PDChatButtonPosition.BottomRight;

	/// <inheritdoc />
	public event Action<bool>? OnMuteStatusChanged;

	/// <inheritdoc />
	public event Action? OnConfigurationChanged;

	/// <inheritdoc />
	public PDChatDockMode RestoreMode
	{
		get => _restoreMode;
		set => SetConfiguration(ref _restoreMode, value);
	}

	/// <inheritdoc />
	public PDChatButtonPosition MinimizedButtonPosition
	{
		get => _minimizedButtonPosition;
		set => SetConfiguration(ref _minimizedButtonPosition, value);
	}

	/// <inheritdoc />
	public bool IsMuted
	{
		get => _isMuted;
		set
		{
			if (SetField(ref _isMuted, value))
			{
				OnMuteStatusChanged?.Invoke(value);
			}
		}
	}

	/// <inheritdoc />
	public string Title
	{
		get => _title;
		set => SetConfiguration(ref _title, value);
	}

	/// <inheritdoc />
	public bool IsMaximizePermitted
	{
		get => _isMaximizePermitted;
		set => SetConfiguration(ref _isMaximizePermitted, value);
	}

	/// <inheritdoc />
	public bool IsCanvasUsePermitted
	{
		get => _isCanvasUsePermitted;
		set => SetConfiguration(ref _isCanvasUsePermitted, value);
	}

	/// <inheritdoc />
	public bool IsClearPermitted
	{
		get => _isClearPermitted;
		set => SetConfiguration(ref _isClearPermitted, value);
	}

	/// <inheritdoc />
	public bool IsInputPermitted
	{
		get => _isInputPermitted;
		set => SetConfiguration(ref _isInputPermitted, value);
	}

	/// <inheritdoc />
	public string? InputDisabledMessage
	{
		get => _inputDisabledMessage;
		set => SetConfiguration(ref _inputDisabledMessage, value);
	}

	/// <inheritdoc />
	public bool AutoRestoreOnNewMessage
	{
		get => _autoRestoreOnNewMessage;
		set => SetConfiguration(ref _autoRestoreOnNewMessage, value);
	}

	/// <inheritdoc />
	public bool UseFullWidthMessages
	{
		get => _useFullWidthMessages;
		set => SetConfiguration(ref _useFullWidthMessages, value);
	}

	/// <inheritdoc />
	public MessageMetadataDisplayMode MessageMetadataDisplayMode
	{
		get => _messageMetadataDisplayMode;
		set => SetConfiguration(ref _messageMetadataDisplayMode, value);
	}

	/// <inheritdoc />
	public bool ShowMessageUserIcon
	{
		get => _showMessageUserIcon;
		set => SetConfiguration(ref _showMessageUserIcon, value);
	}

	/// <inheritdoc />
	public bool ShowMessageUserName
	{
		get => _showMessageUserName;
		set => SetConfiguration(ref _showMessageUserName, value);
	}

	/// <inheritdoc />
	public bool ShowMessageTimestamp
	{
		get => _showMessageTimestamp;
		set => SetConfiguration(ref _showMessageTimestamp, value);
	}

	/// <inheritdoc />
	public string MessageTimestampFormat
	{
		get => _messageTimestampFormat;
		set => SetConfiguration(ref _messageTimestampFormat, value);
	}

	/// <inheritdoc />
	[Obsolete("Superseded by ToastEnabled.")]
	public bool ShowLastMessage
	{
		get => ToastEnabled;
		set => ToastEnabled = value;
	}

	/// <inheritdoc />
	[Obsolete("Superseded by ToastDisplayDurationSeconds.")]
	public double ShowLastMessageDurationSeconds
	{
		get => ToastDisplayDurationSeconds;
		set => ToastDisplayDurationSeconds = value;
	}

	/// <inheritdoc />
	public bool ToastEnabled
	{
		get => _toastEnabled;
		set => SetConfiguration(ref _toastEnabled, value);
	}

	/// <inheritdoc />
	public double ToastDisplayDurationSeconds
	{
		get => _toastDisplayDurationSeconds;
		set => SetConfiguration(ref _toastDisplayDurationSeconds, value);
	}

	/// <inheritdoc />
	public PDChatToastAnimation ToastEntryAnimation
	{
		get => _toastEntryAnimation;
		set => SetConfiguration(ref _toastEntryAnimation, value);
	}

	/// <inheritdoc />
	public PDChatToastAnimation ToastExitAnimation
	{
		get => _toastExitAnimation;
		set => SetConfiguration(ref _toastExitAnimation, value);
	}

	/// <inheritdoc />
	public double ToastAnimationDurationMs
	{
		get => _toastAnimationDurationMs;
		set => SetConfiguration(ref _toastAnimationDurationMs, value);
	}

	/// <inheritdoc />
	public bool ToastAutoDismiss
	{
		get => _toastAutoDismiss;
		set => SetConfiguration(ref _toastAutoDismiss, value);
	}

	/// <inheritdoc />
	public bool ToastShowTitle
	{
		get => _toastShowTitle;
		set => SetConfiguration(ref _toastShowTitle, value);
	}

	/// <inheritdoc />
	public string ToastMinWidth
	{
		get => _toastMinWidth;
		set => SetConfiguration(ref _toastMinWidth, value);
	}

	/// <inheritdoc />
	public string ToastMaxWidth
	{
		get => _toastMaxWidth;
		set => SetConfiguration(ref _toastMaxWidth, value);
	}

	/// <inheritdoc />
	public string ToastMinHeight
	{
		get => _toastMinHeight;
		set => SetConfiguration(ref _toastMinHeight, value);
	}

	/// <inheritdoc />
	public string ToastMaxHeight
	{
		get => _toastMaxHeight;
		set => SetConfiguration(ref _toastMaxHeight, value);
	}

	/// <inheritdoc />
	public int ToastMaxVisible
	{
		get => _toastMaxVisible;
		set => SetConfiguration(ref _toastMaxVisible, value);
	}

	/// <inheritdoc />
	public PDChatButtonPosition ToastAnchor
	{
		get => _toastAnchor;
		set => SetConfiguration(ref _toastAnchor, value);
	}

	/// <summary>
	/// Stores a setting and announces it through <see cref="OnConfigurationChanged"/>, if it has changed.
	/// </summary>
	private void SetConfiguration<T>(ref T field, T value)
	{
		if (SetField(ref field, value))
		{
			OnConfigurationChanged?.Invoke();
		}
	}

	/// <summary>
	/// Stores a duration setting and announces it through <see cref="OnConfigurationChanged"/>, if it has changed
	/// by more than rounding noise.
	/// </summary>
	private void SetConfiguration(ref double field, double value)
	{
		if (Math.Abs(field - value) > 0.001)
		{
			field = value;
			OnConfigurationChanged?.Invoke();
		}
	}

	/// <summary>Stores a value, reporting whether it differed from the one it replaced.</summary>
	private static bool SetField<T>(ref T field, T value)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}

		field = value;
		return true;
	}
}
