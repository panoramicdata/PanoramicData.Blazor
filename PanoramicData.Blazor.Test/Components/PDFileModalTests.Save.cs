using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Save mode and overwrite confirmation tests for <see cref="PDFileModal"/>.
/// </summary>
public partial class PDFileModalTests
{
	/// <summary>Verifies that save mode shows the save title and button, and the initial filename.</summary>
	[Fact]
	public async Task Save_mode_shows_the_initial_filename()
	{
		var component = RenderModal();

		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync("/new.md"));

		component.Find(".modal-title").TextContent.Should().Be("File Save");
		OkButton(component).TextContent.Trim().Should().Be("Save");
		OkButton(component).HasAttribute("disabled").Should().BeFalse();
		FilenameBox(component).GetAttribute("value").Should().Be("new.md");
		FilenameBox(component).ClassList.Should().NotContain("d-none");
	}

	/// <summary>Verifies that saving reports the current folder joined with the filename.</summary>
	[Fact]
	public async Task Saving_reports_the_folder_and_filename()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"), Patience);

		await TypeFilenameAsync(component, "new.md");
		await OkButton(component).ClickAsync(new());

		_results.Should().Equal("/new.md");
	}

	/// <summary>Verifies that the Save button follows whether a filename has been typed.</summary>
	[Fact]
	public async Task The_save_button_follows_the_typed_filename()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());

		await TypeFilenameAsync(component, "new.md");
		OkButton(component).HasAttribute("disabled").Should().BeFalse();

		await TypeFilenameAsync(component, " ");
		OkButton(component).HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Verifies that pressing Enter in the filename box saves.</summary>
	[Fact]
	public async Task Enter_in_the_filename_box_saves()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"), Patience);

		await TypeFilenameAsync(component, "new.md");
		await FilenameBox(component).KeyUpAsync(new KeyboardEventArgs { Code = "Enter", Key = "Enter" });

		component.WaitForAssertion(() => _results.Should().Equal("/new.md"), Patience);
	}

	/// <summary>Verifies that saving over an existing file asks first, and confirming reports the path.</summary>
	[Fact]
	public async Task Confirming_an_overwrite_reports_the_path()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"), Patience);
		await TypeFilenameAsync(component, "readme.txt");

		var save = OkButton(component).ClickAsync(new());
		component.WaitForAssertion(() => _results.Should().BeEmpty(), Patience);
		await ConfirmButton(component, "Yes").ClickAsync(new());
		await save;

		_results.Should().Equal("/readme.txt");
		ConfirmDialog(component).TextContent.Should().Contain("readme.txt");
	}

	/// <summary>Verifies that declining an overwrite reports nothing and leaves the dialog open.</summary>
	[Fact]
	public async Task Declining_an_overwrite_reports_nothing()
	{
		var component = RenderModal();
		await component.InvokeAsync(() => component.Instance.ShowSaveAsAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"), Patience);
		await TypeFilenameAsync(component, "readme.txt");

		var save = OkButton(component).ClickAsync(new());
		await ConfirmButton(component, "No").ClickAsync(new());
		await save;

		_results.Should().BeEmpty();
	}
}
