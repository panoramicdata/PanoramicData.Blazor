namespace PanoramicData.Blazor.Models.Monaco;

public class Language
{
	/// <summary>
	/// Gets or sets the unique identifier of the language.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Are there Methods added to the method cache for this language that should be shown?
	/// </summary>
	/// <remarks>If set to true then you should supply method for the InitializeCache parameter of the PDMonacoEditor.</remarks>
	public bool ShowCompletions { get; set; }

	/// <summary>
	/// If completions are shown and method signatures added to the cache,then these characters will
	/// pop open the signature helper dialog.
	/// </summary>
	public char[] SignatureHelpTriggers { get; set; } = Array.Empty<char>();
}
