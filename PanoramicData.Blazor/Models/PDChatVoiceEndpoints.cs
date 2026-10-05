namespace PanoramicData.Blazor.Models;

/// <summary>
/// The host's two websocket endpoints behind <see cref="PanoramicData.Blazor.PDChat"/>'s Voice Mode.
/// </summary>
/// <param name="ListenUrl">
/// Receives 24 kHz mono float32 audio as binary messages, and answers with JSON text messages:
/// <c>{"type":"word","text":...}</c> as words are recognised, <c>{"type":"turn","text":...}</c> once the speaker has
/// finished, and <c>{"type":"error","text":...}</c>.
/// </param>
/// <param name="SpeakUrl">
/// Receives one JSON message, <c>{"text":...}</c>, and answers with 24 kHz mono float32 audio as binary messages,
/// then <c>{"type":"done"}</c>.
/// </param>
/// <param name="ModulePath">
/// An optional replacement for the browser module that drives the microphone and plays answers, exporting the same
/// <c>start</c>, <c>pause</c>, <c>speak</c> and <c>stop</c> functions; null uses the standard one.
/// </param>
/// <remarks>Relative URLs are resolved against the page. Speech is processed wherever the host's endpoints send it.</remarks>
public sealed record PDChatVoiceEndpoints(string ListenUrl, string SpeakUrl, string? ModulePath = null)
{
	/// <summary>
	/// Voice Mode with no speech service, for demos: it hears that the user is talking but makes the words up, and
	/// answers in the browser's own voice. No audio leaves the browser.
	/// </summary>
	public static PDChatVoiceEndpoints Simulated { get; } = new("simulated", "simulated", "./_content/PanoramicData.Blazor/js/pdchat-voice-simulated.js");
}
