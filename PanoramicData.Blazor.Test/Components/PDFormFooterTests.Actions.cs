using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Save, cancel and delete tests for <see cref="PDFormFooter{TItem}"/>.
/// </summary>
public partial class PDFormFooterTests
{
	/// <summary>Verifies that saving an item goes through the provider's update or create and reports Save.</summary>
	[Theory]
	[InlineData(FormModes.Edit)]
	[InlineData(FormModes.Create)]
	public async Task Saving_goes_through_the_provider(FormModes mode)
	{
		var provider = new RecordingProvider();
		var (form, person, clicks) = await OpenAsync(provider, mode);

		await PressAsync(form, "Save");

		var saved = mode == FormModes.Edit ? provider.Updated : provider.Created;
		saved.Should().ContainSingle().Which.Should().BeSameAs(person);
		clicks.Should().Equal("Save");
	}

	/// <summary>Verifies that a failed save does not report Save, so the caller does not close the form.</summary>
	[Fact]
	public async Task A_failed_save_does_not_report_save()
	{
		var provider = new RecordingProvider { Succeeds = false };
		var (form, _, clicks) = await OpenAsync(provider, FormModes.Edit);

		await PressAsync(form, "Save");

		provider.Updated.Should().ContainSingle();
		clicks.Should().BeEmpty();
	}

	/// <summary>Verifies that a form with no data provider still reports Save and Yes, leaving the work to the caller.</summary>
	[Theory]
	[InlineData(FormModes.Edit, "Save")]
	[InlineData(FormModes.Delete, "Yes")]
	public async Task A_button_without_a_provider_still_reports_its_key(FormModes mode, string key)
	{
		var (form, _, clicks) = await OpenAsync(null, mode);

		await PressAsync(form, key);

		clicks.Should().Equal(key);
	}

	/// <summary>Verifies that cancelling an unchanged edit reports Cancel at once, with no confirmation.</summary>
	[Fact]
	public async Task Cancelling_an_unchanged_edit_reports_cancel()
	{
		var (form, _, clicks) = await OpenAsync(new RecordingProvider(), FormModes.Edit);

		await PressAsync(form, "Cancel");

		clicks.Should().Equal("Cancel");
		form.Instance.Mode.Should().Be(FormModes.Edit);
	}

	/// <summary>Verifies that cancelling an edit with changes asks for confirmation instead of discarding them.</summary>
	[Fact]
	public async Task Cancelling_a_changed_edit_asks_for_confirmation()
	{
		var (form, _, clicks) = await OpenAsync(new RecordingProvider(), FormModes.Edit);
		form.Instance.Delta["Name"] = "Changed";

		await PressAsync(form, "Cancel");

		clicks.Should().BeEmpty();
		form.Instance.Mode.Should().Be(FormModes.Cancel);
		VisibleButtonTexts(form).Should().Equal("Yes", "No");
	}

	/// <summary>Verifies that confirming a cancel discards the changes and reports Cancel.</summary>
	[Fact]
	public async Task Confirming_a_cancel_discards_the_changes()
	{
		var (form, _, clicks) = await OpenAsync(new RecordingProvider(), FormModes.Edit);
		form.Instance.Delta["Name"] = "Changed";
		await PressAsync(form, "Cancel");

		await PressAsync(form, "Yes");

		clicks.Should().Equal("Cancel");
		form.Instance.Delta.Should().BeEmpty();
	}

	/// <summary>Verifies that declining a cancel returns to the edit with the changes kept, and reports No.</summary>
	[Fact]
	public async Task Declining_a_cancel_returns_to_the_edit()
	{
		var (form, _, clicks) = await OpenAsync(new RecordingProvider(), FormModes.Edit);
		form.Instance.Delta["Name"] = "Changed";
		await PressAsync(form, "Cancel");

		await PressAsync(form, "No");

		clicks.Should().Equal("No");
		form.Instance.Mode.Should().Be(FormModes.Edit);
		form.Instance.Delta.Should().ContainKey("Name");
	}

	/// <summary>Verifies that Delete asks for confirmation before deleting anything.</summary>
	[Fact]
	public async Task Delete_asks_for_confirmation_first()
	{
		var provider = new RecordingProvider();
		var (form, _, _) = await OpenAsync(provider, FormModes.Edit);

		await PressAsync(form, "Delete");

		form.Instance.Mode.Should().Be(FormModes.Delete);
		provider.Deleted.Should().BeEmpty();
	}

	/// <summary>Verifies that confirming a delete deletes through the provider and reports Yes.</summary>
	[Fact]
	public async Task Confirming_a_delete_deletes_through_the_provider()
	{
		var provider = new RecordingProvider();
		var (form, person, clicks) = await OpenAsync(provider, FormModes.Edit);
		await PressAsync(form, "Delete");

		await PressAsync(form, "Yes");

		provider.Deleted.Should().ContainSingle().Which.Should().BeSameAs(person);
		clicks.Should().Equal("Yes");
	}

	/// <summary>Verifies that a failed delete does not report Yes.</summary>
	[Fact]
	public async Task A_failed_delete_does_not_report_yes()
	{
		var provider = new RecordingProvider { Succeeds = false };
		var (form, _, clicks) = await OpenAsync(provider, FormModes.Delete);

		await PressAsync(form, "Yes");

		provider.Deleted.Should().ContainSingle();
		clicks.Should().BeEmpty();
	}
}
