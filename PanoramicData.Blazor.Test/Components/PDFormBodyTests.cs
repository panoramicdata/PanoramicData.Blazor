using System.Linq.Expressions;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDFormBody{TItem}"/> lays out the fields of its form for each form mode, groups them,
/// shows help, helpers and validation state, and can take its fields from a table.
/// </summary>
public class PDFormBodyTests : BunitContext
{
	private readonly Person _person = new() { Id = 1, Name = "Ada", Email = "ada@example.com", Age = 36 };
	private readonly BunitJSModuleInterop _common;

	/// <summary>Sets up the rendering context.</summary>
	public PDFormBodyTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		_common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
	}

	/// <summary>A body with no form around it says so.</summary>
	[Fact]
	public void Without_a_form_the_body_says_so()
	{
		var body = Render<PDFormBody<Person>>();

		body.Markup.Should().Contain("Form parameter has not been set.");
	}

	/// <summary>A hidden form renders no body, and an empty one renders the body with no fields.</summary>
	[Fact]
	public async Task Hidden_and_empty_modes_render_no_fields()
	{
		var host = RenderForm([Field(x => x.Name)]);
		host.FindAll(".pd-form-body").Should().BeEmpty();

		await Edit(host, FormModes.Empty);

		host.Find(".pd-form-body").Should().NotBeNull();
		host.FindAll(".pd-form-field").Should().BeEmpty();
	}

	/// <summary>In edit mode each shown field gets a titled row, and the title width is applied.</summary>
	[Fact]
	public async Task Edit_mode_renders_a_row_per_shown_field()
	{
		var host = RenderForm(
			[Field(x => x.Name, label: "Full name"), Field(x => x.Email, extra: new() { [nameof(PDField<Person>.ShowInEdit)] = (Func<Person?, bool>)(_ => false) })],
			titleWidth: 150);

		await Edit(host, FormModes.Edit);

		var rows = host.FindAll(".pd-form-field");
		rows.Should().ContainSingle();
		rows[0].QuerySelector(".title-box span")!.TextContent.Should().Be("Full name");
		host.Find(".pd-form-body").GetAttribute("style").Should().Be("--title-width: 150px;");
		host.Find("style").TextContent.Should().Contain("min-width: 150px");
		host.Find(".d-table.input-group-text").ClassList.Should().Contain("alert-success");
		host.Find(".d-table i").ClassList.Should().Contain("fa-check-circle");
	}

	/// <summary>A title width of zero sets no width style.</summary>
	[Fact]
	public async Task A_zero_title_width_sets_no_style()
	{
		var host = RenderForm([Field(x => x.Name)], titleWidth: 0);

		await Edit(host, FormModes.Edit);

		host.Find(".pd-form-body").GetAttribute("style").Should().BeEmpty();
	}

	/// <summary>Fields sharing a group share a row, and ungrouped fields get a row each.</summary>
	[Fact]
	public async Task Grouped_fields_share_a_row()
	{
		var host = RenderForm(
		[
			Field(x => x.Name, extra: new() { [nameof(PDField<Person>.Group)] = "who" }),
			Field(x => x.Age),
			Field(x => x.Email, extra: new() { [nameof(PDField<Person>.Group)] = "who" })
		]);

		await Edit(host, FormModes.Edit);

		var rows = host.FindAll(".pd-form-field");
		rows.Select(r => r.Id).Should().Equal("group-1", "group-2");
		host.FindComponents<PDFormFieldEditor<Person>>().Should().HaveCount(3);
	}

	/// <summary>Create and delete modes show the fields marked for them, and delete shows no validation indicator.</summary>
	[Fact]
	public async Task Create_and_delete_modes_use_their_own_visibility()
	{
		var host = RenderForm(
		[
			Field(x => x.Name, extra: new() { [nameof(PDField<Person>.ShowInCreate)] = (Func<Person?, bool>)(_ => false) }),
			Field(x => x.Email, extra: new() { [nameof(PDField<Person>.ShowInDelete)] = (Func<Person?, bool>)(_ => true) })
		]);

		await Edit(host, FormModes.Create);
		host.FindAll(".pd-form-field").Should().HaveCount(1);

		await Edit(host, FormModes.Delete);
		host.FindAll(".pd-form-field").Should().ContainSingle();
		host.FindAll(".d-table.input-group-text").Should().BeEmpty();
	}

	/// <summary>Cancel mode shows only the changed fields that were shown in the mode before it.</summary>
	[Fact]
	public async Task Cancel_mode_shows_only_changed_fields()
	{
		var host = RenderForm([Field(x => x.Name), Field(x => x.Email)]);
		await Edit(host, FormModes.Edit);
		var form = FormOf(host);
		await host.InvokeAsync(() => form.SetFieldValueAsync(form.Fields[1], "new@example.com"));

		await host.InvokeAsync(() => form.EditItemAsync(_person, FormModes.Cancel, false));

		host.FindAll(".pd-form-field").Should().ContainSingle();
		BodyOf(host).IsReadOnly(form.Fields[0]).Should().BeTrue();
	}

	/// <summary>A field in error is flagged and its messages listed.</summary>
	[Fact]
	public async Task Errors_are_flagged_and_listed()
	{
		var host = RenderForm([Field(x => x.Name)]);
		await Edit(host, FormModes.Edit);
		var form = FormOf(host);

		await host.InvokeAsync(() =>
		{
			form.Fields[0].SuppressErrors = false;
			form.SetFieldErrors(nameof(Person.Name), "Too short", "Not allowed");
		});
		host.Render();

		host.Find(".title-box").ClassList.Should().Contain("alert-danger");
		host.FindAll(".pd-form-field-form-error-message").Select(e => e.TextContent).Should().Equal("Too short", "Not allowed");
		host.Find(".d-table i").ClassList.Should().Contain("fa-exclamation-circle");
		BodyOf(host).GetEditorClass(form.Fields[0]).Should().Be("invalid");
	}

	/// <summary>A field whose errors are suppressed shows a warning indicator and no messages.</summary>
	[Fact]
	public async Task Suppressed_errors_show_a_warning_without_messages()
	{
		var host = RenderForm([Field(x => x.Name)]);
		await Edit(host, FormModes.Edit);
		var form = FormOf(host);

		await host.InvokeAsync(() =>
		{
			form.Fields[0].SuppressErrors = true;
			form.SetFieldErrors(nameof(Person.Name), "Hidden");
		});
		host.Render();

		host.Find(".d-table.input-group-text").ClassList.Should().Contain("alert-warning");
		host.Find(".d-table i").ClassList.Should().Contain("fa-asterisk");
		host.FindAll(".pd-form-field-form-error-message").Should().BeEmpty();
		BodyOf(host).GetEditorClass(form.Fields[0]).Should().Be("invalid");
	}

	/// <summary>A read-only field, or one not showing validation, has no validation state; the indicator can be turned off.</summary>
	[Fact]
	public async Task Validation_indicator_options()
	{
		var host = RenderForm(
		[
			Field(x => x.Name, extra: new() { [nameof(PDField<Person>.ReadOnlyInEdit)] = (Func<Person?, bool>)(_ => true) }),
			Field(x => x.Email, extra: new() { [nameof(PDField<Person>.ShowValidationResult)] = false })
		]);
		await Edit(host, FormModes.Edit);

		host.FindAll(".d-table.input-group-text").Should().AllSatisfy(e => e.ClassList.Length.Should().Be(3));

		var hidden = RenderForm([Field(x => x.Name)], showValidationIndicator: false);
		await Edit(hidden, FormModes.Edit);
		hidden.FindAll(".d-table.input-group-text").Should().BeEmpty();
	}

	/// <summary>A description is shown as help text when help is shown, and not when it is toggled off.</summary>
	[Theory]
	[InlineData(HelpTextMode.Shown, true)]
	[InlineData(HelpTextMode.Toggle, false)]
	[InlineData(HelpTextMode.Hidden, false)]
	public async Task Help_text_follows_the_help_mode(HelpTextMode mode, bool shown)
	{
		var host = RenderForm([Field(x => x.Name, extra: new() { [nameof(PDField<Person>.Description)] = "Your name" })], helpTextMode: mode);

		await Edit(host, FormModes.Edit);

		host.FindAll(".small.text-muted").Any(e => e.TextContent == "Your name").Should().Be(shown);
		host.Find(".title-box").GetAttribute("title").Should().Be("Your name");
	}

	/// <summary>A help url shows an icon that opens the page.</summary>
	[Fact]
	public async Task Help_url_icon_opens_the_page()
	{
		var host = RenderForm([Field(x => x.Name, extra: new() { [nameof(PDField<Person>.HelpUrl)] = "https://help/" })]);
		await Edit(host, FormModes.Edit);

		host.Find(".fa-external-link-alt").Click();

		_common.VerifyInvoke("openUrl").Arguments.Should().Equal("https://help/", "pd-help-page");
	}

	/// <summary>A helper shows its item-specific icon and tooltip, and its result sets the field value.</summary>
	[Fact]
	public async Task A_helper_click_sets_the_field_value()
	{
		var helper = new FormFieldHelper<Person>
		{
			IconCssClass = "fa-default",
			IconCssClass2 = p => $"fa-for-{p.Id}",
			ToolTip2 = p => $"Help {p.Name}",
			Click = _ => new FormFieldResult { NewValue = "Grace" }
		};
		var host = RenderForm([Field(x => x.Name, extra: new() { [nameof(PDField<Person>.Helper)] = helper })]);
		await Edit(host, FormModes.Edit);

		var icon = host.Find("i.pd-form-help-icon");
		icon.ClassList.Should().Contain("fa-for-1").And.Contain("cursor-pointer");
		icon.GetAttribute("title").Should().Be("Help Ada");
		icon.Click();

		FormOf(host).Delta.Should().ContainKey(nameof(Person.Name)).WhoseValue.Should().Be("Grace");
	}

	/// <summary>An async helper sets the value, and a cancelled one leaves it alone.</summary>
	[Theory]
	[InlineData(false, true)]
	[InlineData(true, false)]
	public async Task An_async_helper_click_respects_cancel(bool canceled, bool changed)
	{
		var helper = new FormFieldHelper<Person>
		{
			IconCssClass = "fa-async",
			ToolTip = "Pick",
			ClickAsync = _ => Task.FromResult(new FormFieldResult { Canceled = canceled, NewValue = "Async" })
		};
		var host = RenderForm([Field(x => x.Name, extra: new() { [nameof(PDField<Person>.Helper)] = helper })]);
		await Edit(host, FormModes.Edit);

		host.Find("i.fa-async").Click();

		FormOf(host).Delta.ContainsKey(nameof(Person.Name)).Should().Be(changed);
	}

	/// <summary>A helper without a click handler has no pointer and clicking it does nothing.</summary>
	[Fact]
	public async Task A_helper_without_a_click_does_nothing()
	{
		var helper = new FormFieldHelper<Person> { IconCssClass = "fa-static", ToolTip = "Static" };
		var host = RenderForm([Field(x => x.Name, extra: new() { [nameof(PDField<Person>.Helper)] = helper })]);
		await Edit(host, FormModes.Edit);

		var icon = host.Find("i.fa-static");
		icon.ClassList.Should().NotContain("cursor-pointer");
		icon.GetAttribute("title").Should().Be("Static");
		icon.Click();

		FormOf(host).Delta.Should().BeEmpty();
	}

	/// <summary>A field with a copy button shows a clipboard for its value.</summary>
	[Fact]
	public async Task A_copy_button_shows_a_clipboard()
	{
		var host = RenderForm([Field(x => x.Name, extra: new() { [nameof(PDField<Person>.ShowCopyButton)] = (Func<Person?, bool>)(_ => true) })]);

		await Edit(host, FormModes.Edit);

		host.FindComponent<PDClipboard>().Instance.Text.Should().Be("Ada");
	}

	/// <summary>Fields are read-only in delete, cancel and read-only modes, and when marked so for create or edit.</summary>
	[Fact]
	public async Task IsReadOnly_follows_the_mode()
	{
		var host = RenderForm(
		[
			Field(x => x.Name, extra: new() { [nameof(PDField<Person>.ReadOnlyInCreate)] = (Func<Person?, bool>)(_ => true) }),
			Field(x => x.Email)
		]);
		var body = BodyOf(host);
		var form = FormOf(host);

		var results = new List<(FormModes, bool, bool)>();
		foreach (var mode in new[] { FormModes.Create, FormModes.Edit, FormModes.Delete, FormModes.ReadOnly })
		{
			await Edit(host, mode);
			results.Add((mode, body.IsReadOnly(form.Fields[0]), body.IsReadOnly(form.Fields[1])));
		}

		results.Should().Equal(
			(FormModes.Create, true, false),
			(FormModes.Edit, false, false),
			(FormModes.Delete, true, true),
			(FormModes.ReadOnly, true, true));
		body.IsShown(form.Fields[1], FormModes.ReadOnly).Should().BeTrue();
		body.IsShown(form.Fields[1], FormModes.Hidden).Should().BeFalse();
	}

	/// <summary>A form with no fields of its own takes them from a linked table's columns.</summary>
	[Fact]
	public async Task Fields_come_from_a_linked_table()
	{
		var table = Render<PDTable<Person>>(parameters => parameters
			.Add(p => p.DataProvider, new ListDataProviderService<Person>([_person]))
			.Add(p => p.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<PDColumn<Person>>(0);
				builder.AddComponentParameter(1, nameof(PDColumn<Person>.Field), (Expression<Func<Person, object>>)(x => x.Name));
				builder.AddComponentParameter(2, nameof(PDColumn<Person>.Title), "Known as");
				builder.CloseComponent();
			})));
		table.WaitForAssertion(() => table.Instance.Columns.Should().ContainSingle());

		var host = RenderForm([], table: table.Instance);
		await Edit(host, FormModes.Edit);

		FormOf(host).Fields.Should().ContainSingle().Which.Title.Should().Be("Known as");
		host.Find(".title-box span").TextContent.Should().Be("Known as");
	}

	/// <summary>Disposing the body releases its JavaScript module without error.</summary>
	[Fact]
	public async Task Dispose_releases_the_module()
	{
		var host = RenderForm([Field(x => x.Name)]);

		await FluentActions.Invoking(() => BodyOf(host).DisposeAsync().AsTask()).Should().NotThrowAsync();
	}

	private async Task Edit(IRenderedComponent<PDForm<Person>> host, FormModes mode)
	{
		var form = host.Instance;
		await host.InvokeAsync(() => form.EditItemAsync(_person, mode));
	}

	private static PDForm<Person> FormOf(IRenderedComponent<PDForm<Person>> host) => host.Instance;

	private static PDFormBody<Person> BodyOf(IRenderedComponent<PDForm<Person>> host) => host.FindComponent<PDFormBody<Person>>().Instance;

	private IRenderedComponent<PDForm<Person>> RenderForm(
		FieldSpec[] fields,
		int titleWidth = 200,
		bool showValidationIndicator = true,
		HelpTextMode helpTextMode = HelpTextMode.Toggle,
		PDTable<Person>? table = null)
		=> Render<PDForm<Person>>(parameters => parameters
			.Add(p => p.HelpTextMode, helpTextMode)
			.Add(p => p.DataProvider, new ListDataProviderService<Person>([_person]))
			.Add(p => p.ChildContent, (RenderFragment)(builder =>
			{
				builder.OpenComponent<PDFormBody<Person>>(0);
				builder.AddComponentParameter(1, nameof(PDFormBody<Person>.TitleWidth), titleWidth);
				builder.AddComponentParameter(2, nameof(PDFormBody<Person>.ShowValidationIndicator), showValidationIndicator);
				builder.AddComponentParameter(3, nameof(PDFormBody<Person>.Table), table);
				builder.AddComponentParameter(4, nameof(PDFormBody<Person>.ChildContent), (RenderFragment)(b => AddFields(b, fields)));
				builder.CloseComponent();
			})));

	private static void AddFields(RenderTreeBuilder builder, FieldSpec[] fields)
	{
		foreach (var spec in fields)
		{
			builder.OpenComponent<PDField<Person>>(0);
			builder.AddComponentParameter(1, nameof(PDField<Person>.Field), spec.Property);
			if (spec.Label is not null)
			{
				builder.AddComponentParameter(2, nameof(PDField<Person>.Title), spec.Label);
			}

			foreach (var (name, value) in spec.Extra)
			{
				builder.AddComponentParameter(3, name, value);
			}

			builder.CloseComponent();
		}
	}

	private static FieldSpec Field(Expression<Func<Person, object>> property, string? label = null, Dictionary<string, object?>? extra = null)
		=> new(property, label, extra ?? []);

	/// <summary>Describes a field to render.</summary>
	/// <param name="Property">The property the field edits.</param>
	/// <param name="Label">An optional title shown in the label box.</param>
	/// <param name="Extra">Further field parameters.</param>
	private sealed record FieldSpec(Expression<Func<Person, object>> Property, string? Label, Dictionary<string, object?> Extra);

	/// <summary>The model edited by the form under test.</summary>
	public sealed class Person
	{
		/// <summary>Gets or sets the key.</summary>
		public int Id { get; set; }

		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets the email address.</summary>
		public string Email { get; set; } = string.Empty;

		/// <summary>Gets or sets the age.</summary>
		public int Age { get; set; }
	}
}
