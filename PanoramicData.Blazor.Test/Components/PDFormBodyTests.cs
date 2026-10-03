using System.Linq.Expressions;
using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDFormBody{TItem}"/> lays out the fields of its form for each form mode, groups them,
/// shows help, helpers and validation state, and can take its fields from a table.
/// </summary>
public partial class PDFormBodyTests : BunitContext
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
		foreach (var mode in new[] { FormModes.Create, FormModes.Edit, FormModes.Delete, FormModes.ReadOnly, FormModes.Empty })
		{
			await Edit(host, mode);
			results.Add((mode, body.IsReadOnly(form.Fields[0]), body.IsReadOnly(form.Fields[1])));
		}

		results.Should().Equal(
			(FormModes.Create, true, false),
			(FormModes.Edit, false, false),
			(FormModes.Delete, true, true),
			(FormModes.ReadOnly, true, true),
			(FormModes.Empty, false, false));
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
