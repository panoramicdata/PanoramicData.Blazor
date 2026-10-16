using AwesomeAssertions;
using PanoramicData.Blazor.Models.Monaco;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="MonacoLanguage"/> maps file extensions to Monaco language ids.
/// </summary>
public class MonacoLanguageTests
{
	/// <summary>Verifies that known extensions map to their Monaco language and unknown or missing names map to plaintext.</summary>
	[Theory]
	[InlineData("Foo.cs", "csharp")]
	[InlineData("src/Page.razor", "razor")]
	[InlineData("a/b/app.json", "json")]
	[InlineData("ci.yml", "yaml")]
	[InlineData("ci.yaml", "yaml")]
	[InlineData("x.csproj", "xml")]
	[InlineData("x.props", "xml")]
	[InlineData("README.md", "markdown")]
	[InlineData("run.ps1", "powershell")]
	[InlineData("site.css", "css")]
	[InlineData("app.TS", "typescript")]
	[InlineData("noextension", "plaintext")]
	[InlineData("weird.zzz", "plaintext")]
	[InlineData(null, "plaintext")]
	public void FromFileName_MapsExtension(string? fileName, string expected)
		=> MonacoLanguage.FromFileName(fileName).Should().Be(expected);
}
