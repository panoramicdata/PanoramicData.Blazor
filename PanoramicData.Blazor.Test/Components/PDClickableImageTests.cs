using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDClickableImage"/> shows its image inline and expands it to a full-screen
/// dialog on click, which the close button dismisses.
/// </summary>
public class PDClickableImageTests : BunitContext
{
	/// <summary>
	/// Verifies that the inline image carries every image parameter and that no dialog is shown initially.
	/// </summary>
	[Fact]
	public void Renders_the_inline_image_with_its_attributes_and_no_dialog()
	{
		var component = RenderImage();

		component.FindAll(".fullscreen-dialog").Should().BeEmpty();

		var image = component.Find(".image-container img");
		image.GetAttribute("src").Should().Be("picture.png");
		image.GetAttribute("alt").Should().Be("A picture");
		image.GetAttribute("title").Should().Be("Picture title");
		image.GetAttribute("class").Should().Be("rounded");
	}

	/// <summary>
	/// Verifies that clicking the inline image opens a full-screen dialog showing the same image.
	/// </summary>
	[Fact]
	public async Task Clicking_the_image_opens_the_full_screen_dialog()
	{
		var component = RenderImage();

		await component.InvokeAsync(() => component.Find(".image-container").ClickAsync(new MouseEventArgs()));

		var dialogImage = component.Find(".fullscreen-dialog img");
		dialogImage.GetAttribute("src").Should().Be("picture.png");
		dialogImage.GetAttribute("alt").Should().Be("A picture");
		component.FindAll("img").Should().HaveCount(2);
	}

	/// <summary>
	/// Verifies that the close button dismisses the full-screen dialog.
	/// </summary>
	[Fact]
	public async Task The_close_button_dismisses_the_dialog()
	{
		var component = RenderImage();
		await component.InvokeAsync(() => component.Find(".image-container").ClickAsync(new MouseEventArgs()));

		await component.InvokeAsync(() => component.Find(".fullscreen-dialog .close-btn").ClickAsync(new MouseEventArgs()));

		component.FindAll(".fullscreen-dialog").Should().BeEmpty();
		component.FindAll("img").Should().ContainSingle();
	}

	private IRenderedComponent<PDClickableImage> RenderImage()
		=> Render<PDClickableImage>(parameters => parameters
			.Add(p => p.ImageSource, "picture.png")
			.Add(p => p.Alt, "A picture")
			.Add(p => p.Title, "Picture title")
			.Add(p => p.CssStyles, "rounded"));
}
