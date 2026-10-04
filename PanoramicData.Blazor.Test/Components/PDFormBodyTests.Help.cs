using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Help text, help link, field helper and copy button tests for <see cref="PDFormBody{TItem}"/>.
/// </summary>
public partial class PDFormBodyTests
{
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
}
