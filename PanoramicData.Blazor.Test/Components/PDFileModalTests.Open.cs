using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Open mode tests for <see cref="PDFileModal"/>.
/// </summary>
public partial class PDFileModalTests
{
	/// <summary>Verifies that open mode shows the open title and button, disabled, with no filename box.</summary>
	[Fact]
	public async Task Open_mode_shows_a_disabled_open_button()
	{
		var component = RenderModal();

		await ShowOpenAsync(component);

		component.Find(".modal-title").TextContent.Should().Be("File Open");
		OkButton(component).TextContent.Trim().Should().Be("Open");
		OkButton(component).HasAttribute("disabled").Should().BeTrue();
		FilenameBox(component).ClassList.Should().Contain("d-none");
	}

	/// <summary>Verifies that selecting a file enables Open, and Open reports the file's path.</summary>
	[Fact]
	public async Task Opening_a_selected_file_reports_its_path()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await Row(component, "/readme.txt").MouseUpAsync(new MouseEventArgs());
		OkButton(component).HasAttribute("disabled").Should().BeFalse();
		await OkButton(component).ClickAsync(new());

		_results.Should().Equal("/readme.txt");
	}

	/// <summary>Verifies that selecting a folder in file mode leaves Open disabled.</summary>
	[Fact]
	public async Task Selecting_a_folder_in_file_mode_leaves_open_disabled()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await Row(component, "/Docs").MouseUpAsync(new MouseEventArgs());

		OkButton(component).HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>Verifies that Cancel reports an empty result.</summary>
	[Fact]
	public async Task Cancel_reports_an_empty_result()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await FooterButton(component, "Cancel").ClickAsync(new());

		_results.Should().Equal(string.Empty);
	}

	/// <summary>Verifies that double-clicking a file opens it at once.</summary>
	[Fact]
	public async Task Double_clicking_a_file_opens_it()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await Row(component, "/readme.txt").DoubleClickAsync(new MouseEventArgs());

		_results.Should().Equal("/readme.txt");
	}

	/// <summary>Verifies that double-clicking a folder does not open it as a result.</summary>
	[Fact]
	public async Task Double_clicking_a_folder_reports_nothing()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		await Row(component, "/Docs").DoubleClickAsync(new MouseEventArgs());

		_results.Should().BeEmpty();
	}

	/// <summary>Verifies that opening with a filename pattern shows only the files matching it.</summary>
	[Fact]
	public async Task Open_with_a_pattern_shows_only_matching_files()
	{
		var component = RenderModal();

		await component.InvokeAsync(() => component.Instance.ShowOpenAsync(false, "*.md"));

		component.WaitForAssertion(() => component.FindAll("tr[id='/readme.txt']").Should().BeEmpty(), Patience);
		component.WaitForAssertion(() => Row(component, "/Docs"), Patience);
	}
}
