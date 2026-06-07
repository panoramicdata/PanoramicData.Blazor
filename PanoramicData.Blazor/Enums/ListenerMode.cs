namespace PanoramicData.Blazor.Enums;

/// <summary>
/// Defines how speech input is activated and processed.
/// </summary>
public enum ListenerMode
{
	/// <summary>
	/// Speech processing starts and stops only when manually requested.
	/// </summary>
	ManualActivation,

	/// <summary>
	/// Speech processing requires a keyword before active transcription.
	/// </summary>
	KeywordActivation,

	/// <summary>
	/// Speech processing continuously emits recognized phrases.
	/// </summary>
	Continuous
}
