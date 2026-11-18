namespace PanoramicData.Blazor.Models;

/// <summary>An image attached to a <see cref="ChatMessage"/>, held in memory as base64.</summary>
public class ChatImage
{
	/// <summary>Gets the image's media type, such as <c>image/png</c>.</summary>
	public required string ContentType { get; init; }

	/// <summary>Gets the image bytes as base64, without a data URI prefix.</summary>
	public required string Base64Data { get; init; }

	/// <summary>Gets or sets an optional file name or label, for the alt text.</summary>
	public string? Name { get; set; }

	/// <summary>Gets the image as a data URI, ready for an <c>img</c> element.</summary>
	public string DataUri => $"data:{ContentType};base64,{Base64Data}";
}
