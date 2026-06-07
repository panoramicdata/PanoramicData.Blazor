namespace PanoramicData.Blazor.Models;

/// <summary>
/// Defines runtime configuration for listener behavior.
/// </summary>
public class ListenerConfiguration
{
	/// <summary>
	/// Gets or sets the listener mode.
	/// </summary>
	public ListenerMode Mode { get; set; } = ListenerMode.ManualActivation;

	/// <summary>
	/// Gets or sets the activation keyword for keyword mode.
	/// </summary>
	public string Keyword { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the timeout after silence before keyword is required again.
	/// </summary>
	public TimeSpan KeywordSilenceTimeout { get; set; } = TimeSpan.FromSeconds(3);

	/// <summary>
	/// Gets or sets an optional token injected when keyword mode times out.
	/// </summary>
	public string? KeywordTimeoutToken { get; set; }

	/// <summary>
	/// Gets or sets the token injected when manual listening starts.
	/// </summary>
	public string? ManualStartToken { get; set; }

	/// <summary>
	/// Gets or sets the token injected when manual listening stops.
	/// </summary>
	public string? ManualStopToken { get; set; }
}
