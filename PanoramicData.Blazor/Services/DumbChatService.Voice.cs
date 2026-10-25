using System;

namespace PanoramicData.Blazor.Services;

/// <summary>
/// The voice controls and the agent and model pickers of <see cref="DumbChatService"/>, so a demo shows the whole input
/// toolbar without a speech service. Each setting announces a change through <see cref="OnConfigurationChanged"/>.
/// </summary>
public partial class DumbChatService
{
	/// <summary>The wake phrases the demo suggests; its simulated microphone says the first of them every other time.</summary>
	public static IReadOnlyList<string> DemoWakePhrases { get; } = ["Hey DumbBot", "OK DumbBot"];

	/// <summary>The agents offered by default.</summary>
	public static IReadOnlyList<PDChatAgentOption> DemoAgents { get; } =
	[
		new("dumbbot", "DumbBot", "Answers by keyword, and not very well."),
		new("pedant", "Pedant", "Exactly as helpful as DumbBot, but would like to correct your grammar first."),
	];

	/// <summary>The models offered by default. The demo answers the same whichever is chosen.</summary>
	public static IReadOnlyList<PDChatModelOption> DemoModels { get; } =
	[
		new("dumb-mini", "Dumb Mini", "Small and quick."),
		new("dumb-max", "Dumb Max", "Large and thorough. Equally dumb."),
	];

	private PDChatVoiceEndpoints? _voiceEndpoints = PDChatVoiceEndpoints.Simulated;
	private IReadOnlyList<PDChatAgentOption>? _agents = DemoAgents;
	private IReadOnlyList<PDChatModelOption>? _models = DemoModels;
	private IReadOnlyList<string>? _wakePhrases;
	private string? _answeringModelName = "Dumb Model";
	private string? _answeringModelDescription = "The demo's model. It answers the same whatever you ask.";
	private bool _isImageInputOffered = true;
	private string? _imageInputBlockedReason;
	private TimeSpan _voiceIdleTimeout = TimeSpan.FromSeconds(10);
	private TimeSpan _voiceAutoSendDelay = TimeSpan.FromMilliseconds(1000);
	private bool _isReadAloudEnabled;
	private string? _selectedAgentId;
	private string? _selectedModelId;
	private PDChatPickerAction? _agentPickerAction;

	/// <summary>Initializes the demo service, offering <see cref="AgentPickerAction"/> by default.</summary>
	public DumbChatService()
	{
		_agentPickerAction = new PDChatPickerAction("New Agent", AnnounceNewAgentAsync, "[+]", "Where a host would open its page for making an agent.");
	}

	/// <summary>Gets or sets the action ending the agent picker; a demo "[+] New Agent" by default, and null hides it.</summary>
	public PDChatPickerAction? AgentPickerAction
	{
		get => _agentPickerAction;
		set => SetConfiguration(ref _agentPickerAction, value);
	}

	/// <summary>Gets or sets the voice endpoints; simulated by default, and null hides the voice controls.</summary>
	public PDChatVoiceEndpoints? VoiceEndpoints
	{
		get => _voiceEndpoints;
		set => SetConfiguration(ref _voiceEndpoints, value);
	}

	/// <summary>Gets or sets the agents offered in the input toolbar; <see cref="DemoAgents"/> by default.</summary>
	public IReadOnlyList<PDChatAgentOption>? Agents
	{
		get => _agents;
		set => SetConfiguration(ref _agents, value);
	}

	/// <summary>Gets or sets the models offered in the input toolbar; <see cref="DemoModels"/> by default.</summary>
	public IReadOnlyList<PDChatModelOption>? Models
	{
		get => _models;
		set => SetConfiguration(ref _models, value);
	}

	/// <summary>Gets or sets the model named in the toolbar after the agent picker; "Dumb Model" by default, and null shows none.</summary>
	public string? AnsweringModelName
	{
		get => _answeringModelName;
		set => SetConfiguration(ref _answeringModelName, value);
	}

	/// <summary>Gets or sets the tooltip of <see cref="AnsweringModelName"/>.</summary>
	public string? AnsweringModelDescription
	{
		get => _answeringModelDescription;
		set => SetConfiguration(ref _answeringModelDescription, value);
	}

	/// <summary>Gets or sets whether images can be added to a message; true by default in the demo.</summary>
	public bool IsImageInputOffered
	{
		get => _isImageInputOffered;
		set => SetConfiguration(ref _isImageInputOffered, value);
	}

	/// <summary>Gets or sets why images cannot be sent, or null when they can.</summary>
	public string? ImageInputBlockedReason
	{
		get => _imageInputBlockedReason;
		set => SetConfiguration(ref _imageInputBlockedReason, value);
	}

	/// <inheritdoc />
	public IReadOnlyList<string>? WakePhrases
	{
		get => _wakePhrases;
		set => SetConfiguration(ref _wakePhrases, value);
	}

	/// <inheritdoc />
	public TimeSpan VoiceIdleTimeout
	{
		get => _voiceIdleTimeout;
		set => SetConfiguration(ref _voiceIdleTimeout, value);
	}

	/// <inheritdoc />
	public TimeSpan VoiceAutoSendDelay
	{
		get => _voiceAutoSendDelay;
		set => SetConfiguration(ref _voiceAutoSendDelay, value);
	}

	/// <inheritdoc />
	public bool IsReadAloudEnabled
	{
		get => _isReadAloudEnabled;
		set => SetConfiguration(ref _isReadAloudEnabled, value);
	}

	/// <inheritdoc />
	public string? SelectedAgentId
	{
		get => _selectedAgentId;
		set => SetConfiguration(ref _selectedAgentId, value);
	}

	/// <inheritdoc />
	public string? SelectedModelId
	{
		get => _selectedModelId;
		set => SetConfiguration(ref _selectedModelId, value);
	}

	private Task AnnounceNewAgentAsync()
	{
		Deliver(_activeConversationId, new ChatMessage
		{
			Id = Guid.NewGuid(),
			Sender = DumbBot,
			AuthorImageSvg = DumbBotFaceSvg,
			Message = "A host would open its page for making a new agent here. Your agent is unchanged.",
			Type = MessageType.Normal,
			Timestamp = DateTime.UtcNow,
		});
		return Task.CompletedTask;
	}
}
