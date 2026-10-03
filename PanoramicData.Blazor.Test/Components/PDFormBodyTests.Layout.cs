using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDFormBody{TItem}"/> lays out the fields shown in each form mode.
/// </summary>
public partial class PDFormBodyTests
{
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
}
