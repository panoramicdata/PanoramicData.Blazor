using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Opening a <see cref="PDForm{TItem}"/> makes as few browser round trips as it can, because each one is slow
/// when the browser is far from the server.
/// </summary>
public partial class PDFormTests
{
	private BunitJSModuleInterop SetupCommonModule()
	{
		var common = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		common.Mode = JSRuntimeMode.Loose;
		return common;
	}

	private void RenderEditor(IRenderedComponent<PDForm<Person>> form, FormField<Person> field)
		=> Render<PDFormFieldEditor<Person>>(parameters => parameters
			.Add(p => p.Form, form.Instance)
			.Add(p => p.Field, field));

	/// <summary>Opening a form clears the inline style of every editor it shows in a single call.</summary>
	[Fact]
	public async Task EditItemAsync_ClearsTheEditorsInOneCall()
	{
		var common = SetupCommonModule();
		var form = RenderForm();
		RenderEditor(form, AddField(form, p => p.Name));
		RenderEditor(form, AddField(form, p => p.Notes));
		RenderEditor(form, AddField(form, p => p.Age));

		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);

		common.Invocations["clearInlineStyles"].Should().ContainSingle()
			.Which.Arguments.Should().ContainSingle()
			.Which.Should().BeOfType<ElementReference[]>()
			.Which.Should().HaveCount(3);
		common.Invocations["clearInlineStyle"].Should().BeEmpty();
	}

	/// <summary>An editor the browser has not shown yet cannot have been resized, so it is not cleared.</summary>
	[Fact]
	public async Task EditItemAsync_SkipsEditorsNotYetShown()
	{
		var common = SetupCommonModule();
		var form = RenderForm();
		RenderEditor(form, AddField(form, p => p.Name));
		form.Instance.RegisterFieldEditor(new PDFormFieldEditor<Person>());

		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);

		common.Invocations["clearInlineStyles"].Should().ContainSingle()
			.Which.Arguments.Should().ContainSingle()
			.Which.Should().BeOfType<ElementReference[]>()
			.Which.Should().ContainSingle();
	}

	/// <summary>Opening a form with no unsaved changes leaves the unload guard alone.</summary>
	[Fact]
	public async Task EditItemAsync_WithNoChanges_DoesNotTouchTheUnloadGuard()
	{
		var form = RenderForm();
		AddField(form, p => p.Name);

		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);
		await EditAsync(form, new Person { Name = "Bob" }, FormModes.Edit);

		_module.Invocations["setUnloadListener"].Should().BeEmpty();
	}

	/// <summary>Opening a form after an unsaved change disarms the unload guard once, and only once.</summary>
	[Fact]
	public async Task EditItemAsync_AfterAChange_DisarmsTheUnloadGuardOnce()
	{
		var form = RenderForm();
		var field = AddField(form, p => p.Name);
		await EditAsync(form, new Person { Name = "Ann" }, FormModes.Edit);
		await form.InvokeAsync(() => form.Instance.SetFieldValueAsync(field, "Bob"));

		await EditAsync(form, new Person { Name = "Cy" }, FormModes.Edit);
		await EditAsync(form, new Person { Name = "Di" }, FormModes.Edit);

		UnloadListenerCalls(true).Should().Be(1);
		UnloadListenerCalls(false).Should().Be(1);
	}
}
