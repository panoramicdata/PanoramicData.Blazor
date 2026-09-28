using AwesomeAssertions;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="BlockOverlayService"/>.</summary>
public class BlockOverlayServiceTests
{
	/// <summary>Show without content raises the show event with no html.</summary>
	[Fact]
	public void Show_RaisesShowWithNullHtml()
	{
		var service = new BlockOverlayService();
		var raised = 0;
		string? html = "unset";
		service.OnShow += h => { raised++; html = h; };

		service.Show();

		raised.Should().Be(1);
		html.Should().BeNull();
	}

	/// <summary>Show with content raises the show event with that html.</summary>
	[Fact]
	public void ShowWithHtml_RaisesShowWithHtml()
	{
		var service = new BlockOverlayService();
		string? html = null;
		service.OnShow += h => html = h;

		service.Show("<b>Busy</b>");

		html.Should().Be("<b>Busy</b>");
	}

	/// <summary>Hide raises the hide event.</summary>
	[Fact]
	public void Hide_RaisesHide()
	{
		var service = new BlockOverlayService();
		var hidden = 0;
		service.OnHide += () => hidden++;

		service.Hide();

		hidden.Should().Be(1);
	}

	/// <summary>Showing and hiding with no subscribers does nothing.</summary>
	[Fact]
	public void NoSubscribers_DoesNotThrow()
	{
		var service = new BlockOverlayService();

		var act = () =>
		{
			service.Show();
			service.Show("x");
			service.Hide();
		};

		act.Should().NotThrow();
	}
}
