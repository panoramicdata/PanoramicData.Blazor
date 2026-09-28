using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDImage"/> renders its parameters onto the image element and follows a change event.
/// </summary>
public class PDImageTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDImageTests() => JSInterop.Mode = JSRuntimeMode.Loose;

	/// <summary>Every parameter is reflected on the rendered image element.</summary>
	[Fact]
	public void Parameters_AreRenderedOntoTheImage()
	{
		var component = Render<PDImage>(parameters => parameters
			.Add(p => p.Id, "logo")
			.Add(p => p.CssClass, "rounded")
			.Add(p => p.ToolTip, "Company logo")
			.Add(p => p.Width, "120px")
			.Add(p => p.Value, "logo.png"));

		var img = component.Find("img");
		img.Id.Should().Be("logo");
		img.GetAttribute("src").Should().Be("logo.png");
		img.GetAttribute("title").Should().Be("Company logo");
		img.GetAttribute("style").Should().Be("width: 120px");
		img.ClassList.Should().Contain(["form-control", "rounded"]);
		img.ClassList.Should().NotContain("d-none");
	}

	/// <summary>A hidden image is given the d-none class rather than being removed.</summary>
	[Fact]
	public void IsVisibleFalse_AddsDNone()
	{
		var component = Render<PDImage>(parameters => parameters
			.Add(p => p.IsVisible, false));

		component.Find("img").ClassList.Should().Contain("d-none");
	}

	/// <summary>The default width is Auto.</summary>
	[Fact]
	public void DefaultWidth_IsAuto()
	{
		var component = Render<PDImage>();

		component.Find("img").GetAttribute("style").Should().Be("width: Auto");
	}

	/// <summary>A change event replaces the source, and a null value clears it.</summary>
	[Fact]
	public void ChangeEvent_UpdatesTheSource()
	{
		var component = Render<PDImage>(parameters => parameters
			.Add(p => p.Value, "first.png"));

		component.Find("img").Change("second.png");
		component.Instance.Value.Should().Be("second.png");
		component.Find("img").GetAttribute("src").Should().Be("second.png");

		component.Find("img").Change((object?)null);
		component.Instance.Value.Should().BeEmpty();
	}
}
