namespace PanoramicData.Blazor.Models.Monaco;

public class Language
{
	/// <summary>
	/// Gets or sets the unique identifier of the language.
	/// </summary>
	public string Id { get; set; } = string.Empty;

	/// <summary>
	/// Opening delimiter for a function - normally does not need to be changed.
	/// </summary>
	public char FunctionDelimiter { get; set; } = '(';

	/// <summary>
	/// Postfix character following an optional parameter. e.g. in C# func(value: 123) so : would be postfix char
	/// </summary>
	public char? OptionalParameterPostfix { get; set; }

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
