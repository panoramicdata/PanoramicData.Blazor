using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Enums;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFormFooter{TItem}"/> shows the buttons appropriate to its form's mode, reports
/// validation errors, and drives the save, delete and cancel journeys through the form.
/// </summary>
public class PDFormFooterTests : BunitContext
{
	/// <summary>
	/// How long to wait for a render that another thread or a timer brings about. Generous because a busy
	/// machine (the whole suite under coverage) can hold the renderer's dispatcher well past bUnit's default.
	/// </summary>
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

	/// <summary>Sets up the rendering context.</summary>
	public PDFormFooterTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	/// <summary>Verifies that a footer with no form says so rather than rendering buttons that cannot work.</summary>
	[Fact]
	public void A_footer_without_a_form_says_so()
	{
		var component = Render<PDFormFooter<Person>>();

		component.Markup.Should().Contain("Form parameter has not been set.");
	}

	/// <summary>Verifies that a hidden or empty form renders no footer at all.</summary>
	[Theory]
	[InlineData(FormModes.Hidden)]
	[InlineData(FormModes.Empty)]
	public void A_hidden_or_empty_form_renders_no_footer(FormModes mode)
	{
		var (form, _) = RenderForm(new RecordingProvider(), mode);

		form.FindAll(".pd-form-footer").Should().BeEmpty();
	}

	/// <summary>Verifies which buttons are visible in each form mode.</summary>
	[Theory]
	[InlineData(FormModes.Edit, "Delete,Save,Cancel")]
	[InlineData(FormModes.Create, "Save,Cancel")]
	[InlineData(FormModes.Delete, "Yes,No")]
	[InlineData(FormModes.Cancel, "Yes,No")]
	[InlineData(FormModes.ReadOnly, "Close")]
	public async Task Each_mode_shows_its_own_buttons(FormModes mode, string expected)
	{
		var (form, _) = RenderForm(new RecordingProvider());

		await EditAsync(form, mode);

		VisibleButtonTexts(form).Should().Equal(expected.Split(','));
	}

	/// <summary>Verifies that the Show flags hide the buttons they name.</summary>
	[Fact]
	public async Task The_show_flags_hide_their_buttons()
	{
		var (form, _) = RenderForm(new RecordingProvider(), configure: footer => footer
			.Add(p => p.ShowSave, false)
			.Add(p => p.ShowDelete, false)
			.Add(p => p.ShowCancel, false));

		await EditAsync(form, FormModes.Edit);

		VisibleButtonTexts(form).Should().BeEmpty();
	}

	/// <summary>Verifies that ShowCancelWhenReadOnly false removes the Close button from a read-only form.</summary>
	[Fact]
	public async Task A_read_only_form_can_hide_its_close_button()
	{
		var (form, _) = RenderForm(new RecordingProvider(), configure: footer => footer
			.Add(p => p.ShowCancelWhenReadOnly, false));

		await EditAsync(form, FormModes.ReadOnly);

		VisibleButtonTexts(form).Should().BeEmpty();
	}

	/// <summary>Verifies that button texts can be replaced.</summary>
	[Fact]
	public async Task Button_texts_can_be_replaced()
	{
		var (form, _) = RenderForm(new RecordingProvider(), configure: footer => footer
			.Add(p => p.SaveButtonText, "Store")
			.Add(p => p.CancelButtonText, "Abort")
			.Add(p => p.DeleteButtonText, "Remove"));

		await EditAsync(form, FormModes.Edit);

		VisibleButtonTexts(form).Should().Equal("Remove", "Store", "Abort");
	}

	/// <summary>
	/// Verifies that field errors are counted, named by field title, and disable the Save button.
	/// </summary>
	[Fact]
	public async Task Errors_are_counted_named_and_disable_save()
	{
		var (form, _) = RenderForm(new RecordingProvider());
		await EditAsync(form, FormModes.Edit);
		form.Instance.Fields.Add(new FormField<Person> { Field = x => x.Name, Title = "Full name" });

		await form.InvokeAsync(() => form.Instance.SetFieldErrors("Name", "Required", "Too short"));

		form.WaitForAssertion(() => form.Find(".pd-form-footer-errors").TextContent.Should()
			.Contain("Please correct the highlighted fields"));
		ButtonFor(form, "Save").HasAttribute("disabled").Should().BeTrue();
	}

	/// <summary>
	/// Verifies that the error message placeholders receive the count, the plural suffix and the field titles.
	/// </summary>
	[Theory]
	[InlineData(1, "1 error: Full name")]
	[InlineData(2, "2 errors: Full name")]
	public async Task The_error_message_placeholders_are_filled(int errorCount, string expected)
	{
		var (form, _) = RenderForm(new RecordingProvider(), configure: footer => footer
			.Add(p => p.ErrorCountMessage, "{0} error{1}: {2}"));
		await EditAsync(form, FormModes.Edit);
		form.Instance.Fields.Add(new FormField<Person> { Field = x => x.Name, Title = "Full name" });

		var messages = Enumerable.Range(1, errorCount).Select(i => $"Problem {i}").ToArray();
		await form.InvokeAsync(() => form.Instance.SetFieldErrors("Name", messages));

		form.WaitForAssertion(() => form.Find(".pd-form-footer-errors span").TextContent.Should().Be(expected), Patience);
	}

	/// <summary>Verifies that ShowErrorCount false suppresses the message but still disables Save.</summary>
	[Fact]
	public async Task The_error_count_can_be_hidden()
	{
		var (form, _) = RenderForm(new RecordingProvider(), configure: footer => footer
			.Add(p => p.ShowErrorCount, false));
		await EditAsync(form, FormModes.Edit);

		await form.InvokeAsync(() => form.Instance.SetFieldErrors("Name", "Required"));

		form.WaitForAssertion(() => ButtonFor(form, "Save").HasAttribute("disabled").Should().BeTrue(), Patience);
		form.FindAll(".pd-form-footer-errors").Should().BeEmpty();
	}

	/// <summary>Verifies that clearing the errors removes the message and re-enables Save.</summary>
	[Fact]
	public async Task Clearing_errors_removes_the_message()
	{
		var (form, _) = RenderForm(new RecordingProvider());
		await EditAsync(form, FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldErrors("Name", "Required"));

		await form.InvokeAsync(() => form.Instance.ClearErrors("Name"));

		form.WaitForAssertion(() => form.FindAll(".pd-form-footer-errors").Should().BeEmpty(), Patience);
		ButtonFor(form, "Save").HasAttribute("disabled").Should().BeFalse();
	}

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

	/// <summary>Verifies that a disposed footer stops listening for the form's error changes.</summary>
	[Fact]
	public async Task A_disposed_footer_stops_listening_for_errors()
	{
		var (form, _) = RenderForm(new RecordingProvider());
		await EditAsync(form, FormModes.Edit);
		var footer = form.FindComponent<PDFormFooter<Person>>();

		footer.Instance.Dispose();
		await form.InvokeAsync(() => form.Instance.SetFieldErrors("Name", "Required"));
		footer.Render();

		footer.FindAll(".pd-form-footer-errors").Should().BeEmpty();
	}

	private (IRenderedComponent<PDForm<Person>> Form, Person Person) RenderForm(
		RecordingProvider? provider,
		FormModes mode = FormModes.Create,
		Action<ComponentParameterCollectionBuilder<PDFormFooter<Person>>>? configure = null)
	{
		var person = new Person { Name = "Ada" };
		var form = Render<PDForm<Person>>(parameters =>
		{
			parameters
				.Add(p => p.DefaultMode, mode)
				.Add(p => p.Item, person)
				.AddChildContent<PDFormFooter<Person>>(footer => configure?.Invoke(footer));
			if (provider is not null)
			{
				parameters.Add(p => p.DataProvider, provider);
			}
		});
		return (form, person);
	}

	private static Task EditAsync(IRenderedComponent<PDForm<Person>> form, FormModes mode)
		=> form.InvokeAsync(() => form.Instance.EditItemAsync(form.Instance.Item, mode));

	private static IElement ButtonFor(IRenderedComponent<PDForm<Person>> form, string text)
		=> form.FindAll(".pdtoolbaritem:not(.pd-hidden) button").Single(b => b.TextContent.Trim() == text);

	private static List<string> VisibleButtonTexts(IRenderedComponent<PDForm<Person>> form)
		=> [.. form.FindAll(".pdtoolbaritem:not(.pd-hidden) button").Select(b => b.TextContent.Trim())];

	/// <summary>The item edited by the form.</summary>
	public sealed class Person
	{
		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = string.Empty;
	}

	/// <summary>A data provider that records what it was asked to do and succeeds or fails on request.</summary>
	private sealed class RecordingProvider : DataProviderBase<Person>
	{
		public bool Succeeds { get; init; } = true;

		public List<Person> Created { get; } = [];

		public List<Person> Updated { get; } = [];

		public List<Person> Deleted { get; } = [];

		public override Task<OperationResponse> CreateAsync(Person item, CancellationToken cancellationToken)
			=> Record(Created, item, cancellationToken);

		public override Task<OperationResponse> UpdateAsync(Person item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(delta);
			return Record(Updated, item, cancellationToken);
		}

		public override Task<OperationResponse> DeleteAsync(Person item, CancellationToken cancellationToken)
			=> Record(Deleted, item, cancellationToken);

		private Task<OperationResponse> Record(List<Person> log, Person item, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			log.Add(item);
			return Task.FromResult(new OperationResponse { Success = Succeeds, ErrorMessage = Succeeds ? string.Empty : "Refused" });
		}
	}
}
