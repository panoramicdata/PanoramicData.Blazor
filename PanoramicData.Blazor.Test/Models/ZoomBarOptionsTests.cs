using AwesomeAssertions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Models;

/// <summary>Tests for <see cref="ZoomBarOptions"/> and <see cref="ZoomBarColours"/>.</summary>
public class ZoomBarOptionsTests
{
	/// <summary>By default the zoom steps are every ten percent from 10 to 100.</summary>
	[Fact]
	public void New_HasTenPercentSteps()
	{
		new ZoomBarOptions().ZoomSteps.Should().Equal(10, 20, 30, 40, 50, 60, 70, 80, 90, 100);
	}

	/// <summary>Zoom steps are stored in ascending order whatever order they are supplied in.</summary>
	[Fact]
	public void ZoomSteps_AreSortedAscending()
	{
		var options = new ZoomBarOptions { ZoomSteps = [200, 50, 100, 25] };

		options.ZoomSteps.Should().Equal(25, 50, 100, 200);
	}

	/// <summary>The default colours are a white bar with a silver border and a green handle with white text.</summary>
	[Fact]
	public void Colours_HaveDocumentedDefaults()
	{
		var colours = new ZoomBarOptions().Colours;

		colours.Background.Should().Be("White");
		colours.Border.Should().Be("Silver");
		colours.HandleBackground.Should().Be("Green");
		colours.HandleForeground.Should().Be("White");
	}

	/// <summary>The colours round-trip.</summary>
	[Fact]
	public void Colours_RoundTrip()
	{
		var options = new ZoomBarOptions
		{
			Colours = new ZoomBarColours { Background = "Black", Border = "Red", HandleBackground = "Blue", HandleForeground = "Yellow" }
		};

		options.Colours.Background.Should().Be("Black");
		options.Colours.Border.Should().Be("Red");
		options.Colours.HandleBackground.Should().Be("Blue");
		options.Colours.HandleForeground.Should().Be("Yellow");
	}
}
