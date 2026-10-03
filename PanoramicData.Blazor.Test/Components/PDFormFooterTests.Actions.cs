using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Save, cancel and delete tests for <see cref="PDFormFooter{TItem}"/>.
/// </summary>
public partial class PDFormFooterTests
{
	/// <summary>Verifies that saving an edit updates the item through the provider and reports Save.</summary>
	[Fact]
	public async Task Saving_an_edit_updates_through_the_provider()
	{
		var provider = new RecordingProvider();
		var clicks = new List<string>();
		var (form, person) = RenderForm(provider, configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Edit);

		await ButtonFor(form, "Save").ClickAsync(new());

		provider.Updated.Should().ContainSingle().Which.Should().BeSameAs(person);
		clicks.Should().Equal("Save");
	}

	/// <summary>Verifies that saving a new item creates it through the provider and reports Save.</summary>
	[Fact]
	public async Task Saving_a_new_item_creates_through_the_provider()
	{
		var provider = new RecordingProvider();
		var clicks = new List<string>();
		var (form, person) = RenderForm(provider, configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Create);

		await ButtonFor(form, "Save").ClickAsync(new());

		provider.Created.Should().ContainSingle().Which.Should().BeSameAs(person);
		clicks.Should().Equal("Save");
	}

	/// <summary>Verifies that a failed save does not report Save, so the caller does not close the form.</summary>
	[Fact]
	public async Task A_failed_save_does_not_report_save()
	{
		var provider = new RecordingProvider { Succeeds = false };
		var clicks = new List<string>();
		var (form, _) = RenderForm(provider, configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Edit);

		await ButtonFor(form, "Save").ClickAsync(new());

		provider.Updated.Should().ContainSingle();
		clicks.Should().BeEmpty();
	}

	/// <summary>Verifies that a form with no data provider still reports Save, leaving the save to the caller.</summary>
	[Fact]
	public async Task Saving_without_a_provider_still_reports_save()
	{
		var clicks = new List<string>();
		var (form, _) = RenderForm(null, configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Edit);

		await ButtonFor(form, "Save").ClickAsync(new());

		clicks.Should().Equal("Save");
	}

	/// <summary>Verifies that cancelling an unchanged edit reports Cancel at once, with no confirmation.</summary>
	[Fact]
	public async Task Cancelling_an_unchanged_edit_reports_cancel()
	{
		var clicks = new List<string>();
		var (form, _) = RenderForm(new RecordingProvider(), configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Edit);

		await ButtonFor(form, "Cancel").ClickAsync(new());

		clicks.Should().Equal("Cancel");
		form.Instance.Mode.Should().Be(FormModes.Edit);
	}

	/// <summary>Verifies that cancelling an edit with changes asks for confirmation instead of discarding them.</summary>
	[Fact]
	public async Task Cancelling_a_changed_edit_asks_for_confirmation()
	{
		var clicks = new List<string>();
		var (form, _) = RenderForm(new RecordingProvider(), configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Edit);
		form.Instance.Delta["Name"] = "Changed";

		await ButtonFor(form, "Cancel").ClickAsync(new());

		clicks.Should().BeEmpty();
		form.Instance.Mode.Should().Be(FormModes.Cancel);
		VisibleButtonTexts(form).Should().Equal("Yes", "No");
	}

	/// <summary>Verifies that confirming a cancel discards the changes and reports Cancel.</summary>
	[Fact]
	public async Task Confirming_a_cancel_discards_the_changes()
	{
		var clicks = new List<string>();
		var (form, _) = RenderForm(new RecordingProvider(), configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Edit);
		form.Instance.Delta["Name"] = "Changed";
		await ButtonFor(form, "Cancel").ClickAsync(new());

		await ButtonFor(form, "Yes").ClickAsync(new());

		clicks.Should().Equal("Cancel");
		form.Instance.Delta.Should().BeEmpty();
	}

	/// <summary>Verifies that declining a cancel returns to the edit with the changes kept, and reports No.</summary>
	[Fact]
	public async Task Declining_a_cancel_returns_to_the_edit()
	{
		var clicks = new List<string>();
		var (form, _) = RenderForm(new RecordingProvider(), configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Edit);
		form.Instance.Delta["Name"] = "Changed";
		await ButtonFor(form, "Cancel").ClickAsync(new());

		await ButtonFor(form, "No").ClickAsync(new());

		clicks.Should().Equal("No");
		form.Instance.Mode.Should().Be(FormModes.Edit);
		form.Instance.Delta.Should().ContainKey("Name");
	}

	/// <summary>Verifies that Delete asks for confirmation before deleting anything.</summary>
	[Fact]
	public async Task Delete_asks_for_confirmation_first()
	{
		var provider = new RecordingProvider();
		var (form, _) = RenderForm(provider);
		await EditAsync(form, FormModes.Edit);

		await ButtonFor(form, "Delete").ClickAsync(new());

		form.Instance.Mode.Should().Be(FormModes.Delete);
		provider.Deleted.Should().BeEmpty();
	}

	/// <summary>Verifies that confirming a delete deletes through the provider and reports Yes.</summary>
	[Fact]
	public async Task Confirming_a_delete_deletes_through_the_provider()
	{
		var provider = new RecordingProvider();
		var clicks = new List<string>();
		var (form, person) = RenderForm(provider, configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Edit);
		await ButtonFor(form, "Delete").ClickAsync(new());

		await ButtonFor(form, "Yes").ClickAsync(new());

		provider.Deleted.Should().ContainSingle().Which.Should().BeSameAs(person);
		clicks.Should().Equal("Yes");
	}

	/// <summary>Verifies that a failed delete does not report Yes.</summary>
	[Fact]
	public async Task A_failed_delete_does_not_report_yes()
	{
		var provider = new RecordingProvider { Succeeds = false };
		var clicks = new List<string>();
		var (form, _) = RenderForm(provider, configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Delete);

		await ButtonFor(form, "Yes").ClickAsync(new());

		provider.Deleted.Should().ContainSingle();
		clicks.Should().BeEmpty();
	}

	/// <summary>Verifies that confirming a delete on a form with no provider still reports Yes.</summary>
	[Fact]
	public async Task Confirming_a_delete_without_a_provider_reports_yes()
	{
		var clicks = new List<string>();
		var (form, _) = RenderForm(null, configure: footer => footer
			.Add(p => p.Click, (string key) => clicks.Add(key)));
		await EditAsync(form, FormModes.Delete);

		await ButtonFor(form, "Yes").ClickAsync(new());

		clicks.Should().Equal("Yes");
	}
}
