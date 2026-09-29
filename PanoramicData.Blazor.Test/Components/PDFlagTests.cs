using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDFlag"/> renders the flag image for the country code it is given.
/// </summary>
public class PDFlagTests : BunitContext
{
	/// <summary>The image source is the lower-cased code and the alt text the upper-cased one.</summary>
	[Fact]
	public void Renders_the_flag_url_and_alt_text_from_the_country_code()
	{
		var component = Render<PDFlag>(parameters => parameters
			.Add(p => p.CountryCode, "Gb"));

		var img = component.Find("img");
		img.GetAttribute("src").Should().Be("https://flagcdn.com/gb.svg");
		img.GetAttribute("alt").Should().Be("Flag for GB");
	}

	/// <summary>The width defaults to 2em and follows the Width parameter when set.</summary>
	[Theory]
	[InlineData(null, "2em")]
	[InlineData("48px", "48px")]
	public void Width_defaults_to_2em_and_can_be_overridden(string? width, string expected)
	{
		var component = Render<PDFlag>(parameters =>
		{
			parameters.Add(p => p.CountryCode, "fr");
			if (width is not null)
			{
				parameters.Add(p => p.Width, width);
			}
		});

		component.Find("img").GetAttribute("width").Should().Be(expected);
	}
}
