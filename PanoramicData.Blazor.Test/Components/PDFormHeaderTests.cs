using System.ComponentModel.DataAnnotations;
using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Tests that <see cref="PDFormHeader{TItem}"/> titles its form for the mode it is in, and shows help text
/// as the form's help mode allows.
/// </summary>
public class PDFormHeaderTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDFormHeaderTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDForm<Widget>> RenderForm(
		HelpTextMode helpTextMode = HelpTextMode.Toggle,
		Dictionary<string, object>? headerParameters = null)
		=> Render<PDForm<Widget>>(parameters => parameters
			.Add(p => p.DataProvider, new ListDataProviderService<Widget>())
			.Add(p => p.HelpTextMode, helpTextMode)
			.Add(p => p.ChildContent, builder =>
			{
				builder.OpenComponent<PDFormHeader<Widget>>(0);
				var sequence = 1;
				foreach (var parameter in headerParameters ?? [])
				{
					builder.AddAttribute(sequence++, parameter.Key, parameter.Value);
				}

				builder.CloseComponent();
			}));

	private static async Task EditAsync(IRenderedComponent<PDForm<Widget>> form, FormModes mode, Widget? item = null)
		=> await form.InvokeAsync(() => form.Instance.EditItemAsync(item ?? new Widget { Name = "Sprocket" }, mode, validate: false));

	private static Dictionary<string, object> Describing(params (string Key, object Value)[] extra)
	{
		var parameters = new Dictionary<string, object>
		{
			[nameof(PDFormHeader<Widget>.ItemDescription)] = (Func<Widget, string>)(w => w.Name)
		};
		foreach (var (key, value) in extra)
		{
			parameters[key] = value;
		}

		return parameters;
	}

	/// <summary>
	/// Verifies that a header outside any form says so rather than rendering a title.
	/// </summary>
	[Fact]
	public void WithoutAForm_SaysTheParameterIsMissing()
	{
		var header = Render<PDFormHeader<Widget>>();

		header.Markup.Should().Contain("Form parameter has not been set.");
	}

	/// <summary>
	/// Verifies that nothing is rendered while the form is hidden or empty.
	/// </summary>
	[Theory]
	[InlineData(FormModes.Hidden)]
	[InlineData(FormModes.Empty)]
	public async Task HiddenOrEmptyForm_RendersNoHeader(FormModes mode)
	{
		var form = RenderForm();
		await EditAsync(form, mode);

		form.FindAll(".pd-form-header").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies the automatic title for each mode, using the class display name and the item description.
	/// </summary>
	[Theory]
	[InlineData(FormModes.Create, "Create new Gadget")]
	[InlineData(FormModes.Edit, "Edit 'Sprocket'")]
	[InlineData(FormModes.ReadOnly, "Edit 'Sprocket'")]
	[InlineData(FormModes.Delete, "Are you sure you want to delete 'Sprocket'?")]
	[InlineData(FormModes.Cancel, "Cancel and lose these changes?")]
	public async Task AutomaticTitle_DescribesTheMode(FormModes mode, string expected)
	{
		var form = RenderForm(headerParameters: Describing());
		await EditAsync(form, mode);

		form.Find(".pd-form-header").TextContent.Should().Contain(expected);
	}

	/// <summary>
	/// Verifies that without an item description the class display name stands in for the item.
	/// </summary>
	[Fact]
	public async Task AutomaticTitle_WithoutADescription_UsesTheClassName()
	{
		var form = RenderForm();
		await EditAsync(form, FormModes.Delete);

		form.Find(".pd-form-header").TextContent.Should().Contain("delete 'Gadget'");
	}

	/// <summary>
	/// Verifies that a custom title for each mode replaces the automatic one, with the placeholder
	/// substituted by the item description.
	/// </summary>
	[Theory]
	[InlineData(FormModes.Create, nameof(PDFormHeader<Widget>.CreateTitle), "Making {0}", "Making Sprocket")]
	[InlineData(FormModes.Edit, nameof(PDFormHeader<Widget>.EditTitle), "Changing {0}", "Changing Sprocket")]
	[InlineData(FormModes.Delete, nameof(PDFormHeader<Widget>.DeleteTitle), "Remove {0}?", "Remove Sprocket?")]
	[InlineData(FormModes.Cancel, nameof(PDFormHeader<Widget>.CancelTitle), "Abandon {0}?", "Abandon Sprocket?")]
	public async Task CustomTitle_ReplacesTheAutomaticOne(FormModes mode, string parameter, string title, string expected)
	{
		var form = RenderForm(headerParameters: Describing((parameter, title)));
		await EditAsync(form, mode);

		form.Find(".pd-form-header").TextContent.Trim().Should().Be(expected);
	}

	/// <summary>
	/// Verifies that once a change is made the title is flagged, in both create and edit modes.
	/// </summary>
	[Theory]
	[InlineData(FormModes.Create)]
	[InlineData(FormModes.Edit)]
	public async Task Changes_AreFlaggedInTheTitle(FormModes mode)
	{
		var form = RenderForm();
		var field = new FormField<Widget> { Field = w => w.Name };
		form.Instance.Fields.Add(field);
		await EditAsync(form, mode);
		form.FindAll(".changes-made-flag").Should().BeEmpty();

		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Cog"));

		form.Find(".changes-made-flag").TextContent.Should().Be("(changes made)");
	}

	/// <summary>
	/// Verifies that in toggle mode the help icon shows and hides the help text.
	/// </summary>
	[Theory]
	[InlineData(FormModes.Create)]
	[InlineData(FormModes.Edit)]
	public async Task ToggleHelp_ShowsAndHidesTheHelpText(FormModes mode)
	{
		var form = RenderForm(HelpTextMode.Toggle, new() { [nameof(PDFormHeader<Widget>.HelpText)] = "Fill in the name." });
		await EditAsync(form, mode);
		form.Markup.Should().NotContain("Fill in the name.");

		form.Find("i.fa-question-circle").Click();
		form.Find(".pd-form-header .small.text-muted").TextContent.Trim().Should().Be("Fill in the name.");
		form.Instance.ShowHelp.Should().BeTrue();

		form.Find("i.fa-question-circle").Click();
		form.Markup.Should().NotContain("Fill in the name.");
	}

	/// <summary>
	/// Verifies that help text is always shown in shown mode and never in hidden mode, which has no toggle icon.
	/// </summary>
	[Theory]
	[InlineData(HelpTextMode.Shown, true)]
	[InlineData(HelpTextMode.Hidden, false)]
	public async Task FixedHelpModes_ShowOrHideTheHelpText(HelpTextMode helpTextMode, bool shown)
	{
		var form = RenderForm(helpTextMode, new() { [nameof(PDFormHeader<Widget>.HelpText)] = "Fill in the name." });
		await EditAsync(form, FormModes.Edit);

		form.Markup.Contains("Fill in the name.", StringComparison.Ordinal).Should().Be(shown);
		form.FindAll("i.fa-question-circle").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that help text is not shown outside create and edit modes even when help is shown.
	/// </summary>
	[Fact]
	public async Task HelpText_IsNotShownWhenDeleting()
	{
		var form = RenderForm(HelpTextMode.Shown, new() { [nameof(PDFormHeader<Widget>.HelpText)] = "Fill in the name." });
		await EditAsync(form, FormModes.Delete);

		form.Markup.Should().NotContain("Fill in the name.");
	}

	/// <summary>An item edited by the form.</summary>
	[Display(Name = "Gadget")]
	public sealed class Widget
	{
		/// <summary>Gets or sets the name.</summary>
		public string Name { get; set; } = string.Empty;
	}
}
