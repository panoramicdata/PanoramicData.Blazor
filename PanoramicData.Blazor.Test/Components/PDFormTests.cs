using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDForm{TItem}"/> tracks edits as a delta, validates them, and saves, updates and
/// deletes through its data provider.
/// </summary>
public partial class PDFormTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDForm.razor.js";
	private readonly PersonProvider _provider = new();
	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context.</summary>
	public PDFormTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		_module = JSInterop.SetupModule(ModulePath);
	}

	private IRenderedComponent<PDForm<Person>> RenderForm(Action<ComponentParameterCollectionBuilder<PDForm<Person>>>? configure = null)
		=> Render<PDForm<Person>>(parameters =>
		{
			parameters.Add(p => p.DataProvider, _provider);
			configure?.Invoke(parameters);
		});

	private static FormField<Person> AddField(IRenderedComponent<PDForm<Person>> form, Expression<Func<Person, object>> field, Action<FormField<Person>>? configure = null)
	{
		var definition = new FormField<Person> { Field = field };
		configure?.Invoke(definition);
		form.Instance.Fields.Add(definition);
		return definition;
	}

	private static async Task<IRenderedComponent<PDForm<Person>>> EditAsync(IRenderedComponent<PDForm<Person>> form, Person? item, FormModes mode, bool? validate = null)
	{
		await form.InvokeAsync(() => form.Instance.EditItemAsync(item, mode, true, validate));
		return form;
	}

	private int UnloadListenerCalls(bool armed)
		=> _module.Invocations["setUnloadListener"].Count(i => Equals(i.Arguments[1], armed));

	/// <summary>
	/// Verifies that the form wraps its content in a div carrying its CSS class, and starts in its default mode.
	/// </summary>
	[Fact]
	public void Render_WrapsContent_AndStartsInTheDefaultMode()
	{
		var form = RenderForm(p => p
			.Add(x => x.CssClass, "person-form")
			.Add(x => x.DefaultMode, FormModes.Edit)
			.Add(x => x.ChildContent, "<span class=\"inside\">content</span>"));

		form.Find("div.pd-form.person-form span.inside").TextContent.Should().Be("content");
		form.Instance.Mode.Should().Be(FormModes.Edit);
		form.Instance.Id.Should().StartWith("pd-form-");
		form.Instance.HasChanges.Should().BeFalse();
	}

	/// <summary>
	/// Verifies that a <see cref="PDField{TItem}"/> declared inside the form registers itself, copying its
	/// definition and taking the title from the display attribute when none is given.
	/// </summary>
	[Fact]
	public void AddFieldAsync_FromADeclaredField_CopiesTheDefinition()
	{
		var form = RenderForm(p => p.Add(x => x.ChildContent, builder =>
		{
			builder.OpenComponent<PDFormBody<Person>>(0);
			builder.AddAttribute(1, nameof(PDFormBody<Person>.ChildContent), (RenderFragment)(body =>
			{
				body.OpenComponent<PDField<Person>>(0);
				body.AddAttribute(1, nameof(PDField<Person>.Field), (Expression<Func<Person, object>>)(p => p.Name));
				body.AddAttribute(2, nameof(PDField<Person>.MaxLength), (int?)20);
				body.AddAttribute(3, nameof(PDField<Person>.Group), "Main");
				body.AddAttribute(4, nameof(PDField<Person>.IsTextArea), true);
				body.AddAttribute(5, nameof(PDField<Person>.HelpUrl), "https://help");
				body.CloseComponent();
			}));
			builder.CloseComponent();
		}));

		var field = form.Instance.Fields.Should().ContainSingle().Subject;
		field.Title.Should().Be("Full name");
		field.MaxLength.Should().Be(20);
		field.Group.Should().Be("Main");
		field.IsTextArea.Should().BeTrue();
		field.HelpUrl.Should().Be("https://help");
		form.Instance.GetField("Name").Should().BeSameAs(field);
	}

	/// <summary>
	/// Verifies that entering create mode validates the item, records the previous mode and suppresses the
	/// errors for display until the first edit.
	/// </summary>
	[Fact]
	public async Task EditItemAsync_Create_ValidatesAndSuppressesInitialErrors()
	{
		var form = RenderForm(p => p.Add(x => x.DefaultMode, FormModes.Edit));
		var field = AddField(form, p => p.Name);

		await EditAsync(form, new Person(), FormModes.Create);

		form.Instance.Mode.Should().Be(FormModes.Create);
		form.Instance.PreviousMode.Should().Be(FormModes.Edit);
		form.Instance.Errors.Should().ContainKey("Name");
		form.Instance.IsValid().Should().BeFalse();
		field.SuppressErrors.Should().BeTrue();
	}

	/// <summary>
	/// Verifies that validation can be turned off, and that the delete mode does not validate by default.
	/// </summary>
	[Theory]
	[InlineData(FormModes.Create, false)]
	[InlineData(FormModes.Delete, null)]
	public async Task EditItemAsync_WithoutValidation_RecordsNoErrors(FormModes mode, bool? validate)
	{
		var form = RenderForm();
		AddField(form, p => p.Name);

		await EditAsync(form, new Person(), mode, validate);

		form.Instance.Errors.Should().BeEmpty();
		form.Instance.Item.Should().NotBeNull();
	}

	/// <summary>A person edited by the form.</summary>
	[Display(Name = "Person record")]
	public sealed class Person
	{
		/// <summary>Gets or sets the name.</summary>
		[Required]
		[Display(Name = "Full name")]
		public string Name { get; set; } = string.Empty;

		/// <summary>Gets or sets the age.</summary>
		public int Age { get; set; }

		/// <summary>Gets or sets free text notes.</summary>
		public string Notes { get; set; } = string.Empty;

		/// <summary>Gets or sets an optional nickname.</summary>
		public string? Nickname { get; set; }

		/// <summary>Gets or sets the date of birth.</summary>
		public DateTime Born { get; set; }

		/// <summary>Gets or sets when the person joined.</summary>
		public DateTimeOffset? Joined { get; set; }
	}

	/// <summary>A provider that records what the form asks of it and answers with a configurable response.</summary>
	private sealed class PersonProvider : DataProviderBase<Person>
	{
		public OperationResponse Response { get; set; } = new() { Success = true };

		public List<Person> Created { get; } = [];

		public List<Person> Deleted { get; } = [];

		public List<Person> Updated { get; } = [];

		public Dictionary<string, object?> LastDelta { get; private set; } = [];

		public override Task<OperationResponse> CreateAsync(Person item, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Created.Add(item);
			return Task.FromResult(Response);
		}

		public override Task<OperationResponse> UpdateAsync(Person item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			Updated.Add(item);
			LastDelta = new Dictionary<string, object?>(delta);
			return Task.FromResult(Response);
		}

		public override Task<OperationResponse> DeleteAsync(Person item, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (Response.Success)
			{
				Deleted.Add(item);
			}

			return Task.FromResult(Response);
		}
	}
}
