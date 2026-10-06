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
	private TimeSpan _voiceIdleTimeout = TimeSpan.FromSeconds(10);
	private TimeSpan _voiceAutoSendDelay = TimeSpan.FromMilliseconds(1000);
	private bool _isReadAloudEnabled;
	private string? _selectedAgentId;
	private string? _selectedModelId;

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
}
