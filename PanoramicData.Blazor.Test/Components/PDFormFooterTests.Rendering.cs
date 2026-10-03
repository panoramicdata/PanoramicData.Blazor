using AngleSharp.Dom;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Button and error message tests for <see cref="PDFormFooter{TItem}"/>.
/// </summary>
public partial class PDFormFooterTests
{
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
}
