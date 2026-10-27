namespace PanoramicData.Blazor.Demo.Services;

/// <summary>The demo user's avatar, an inline image so the demo needs no external request.</summary>
public static class DemoAvatars
{
	/// <summary>A round-cropped-friendly head and shoulders, shown beside the demo user's messages.</summary>
	public static string UserImageUrl { get; } = "data:image/svg+xml;utf8,"
		+ Uri.EscapeDataString("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 64 64\"><rect width=\"64\" height=\"64\" fill=\"#5a6b7b\"/><circle cx=\"32\" cy=\"25\" r=\"11\" fill=\"#ffcc99\"/><ellipse cx=\"32\" cy=\"58\" rx=\"22\" ry=\"16\" fill=\"#ffcc99\"/></svg>");
}