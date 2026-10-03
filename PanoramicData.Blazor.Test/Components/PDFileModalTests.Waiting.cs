using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests of the <see cref="PDFileModal"/> methods that show the dialog and wait for the user's answer.
/// </summary>
public partial class PDFileModalTests
{
	/// <summary>Verifies that waiting for an open returns the selected file's path.</summary>
	[Fact]
	public async Task Waiting_for_an_open_returns_the_selected_path()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		var result = component.InvokeAsync(() => component.Instance.ShowOpenAndWaitResultAsync());
		await Row(component, "/readme.txt").MouseUpAsync(new MouseEventArgs());
		await OkButton(component).ClickAsync(new());

		(await result).Should().Be("/readme.txt");
		_results.Should().BeEmpty();
	}

	/// <summary>Verifies that waiting for an open returns an empty path when cancelled.</summary>
	[Fact]
	public async Task Waiting_for_an_open_returns_empty_when_cancelled()
	{
		var component = RenderModal();
		await ShowOpenAsync(component);

		var result = component.InvokeAsync(() => component.Instance.ShowOpenAndWaitResultAsync(true));
		await FooterButton(component, "Cancel").ClickAsync(new());

		(await result).Should().BeEmpty();
	}

	/// <summary>Verifies that waiting for a save returns the path, after an overwrite has been declined and a new name typed.</summary>
	[Fact]
	public async Task Waiting_for_a_save_asks_again_after_a_declined_overwrite()
	{
		var component = RenderModal();

		var result = component.InvokeAsync(() => component.Instance.ShowSaveAsAndWaitResultAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"), Patience);
		await TypeFilenameAsync(component, "readme.txt");
		await OkButton(component).ClickAsync(new());
		await ConfirmButton(component, "No").ClickAsync(new());
		await TypeFilenameAsync(component, "other.txt");
		await OkButton(component).ClickAsync(new());

		(await result).Should().Be("/other.txt");
	}

	/// <summary>Verifies that waiting for a save returns the path when an overwrite is confirmed.</summary>
	[Fact]
	public async Task Waiting_for_a_save_returns_the_path_when_overwrite_confirmed()
	{
		var component = RenderModal();

		var result = component.InvokeAsync(() => component.Instance.ShowSaveAsAndWaitResultAsync("/readme.txt"));
		component.WaitForAssertion(() => Row(component, "/readme.txt"), Patience);
		await OkButton(component).ClickAsync(new());
		await ConfirmButton(component, "Yes").ClickAsync(new());

		(await result).Should().Be("/readme.txt");
	}

	/// <summary>Verifies that waiting for a save returns an empty path when cancelled.</summary>
	[Fact]
	public async Task Waiting_for_a_save_returns_empty_when_cancelled()
	{
		var component = RenderModal();

		var result = component.InvokeAsync(() => component.Instance.ShowSaveAsAndWaitResultAsync());
		component.WaitForAssertion(() => Row(component, "/readme.txt"), Patience);
		await FooterButton(component, "Cancel").ClickAsync(new());

		(await result).Should().BeEmpty();
	}
}
