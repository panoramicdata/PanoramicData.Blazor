using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// JavaScript module and disposal tests for <see cref="PDZoomBar"/>.
/// </summary>
public partial class PDZoomBarTests
{
	/// <summary>On first render the module is initialised with the canvas id, value and options.</summary>
	[Fact]
	public void FirstRender_InitialisesTheModule()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var cut = RenderZoomBar(40);

		var init = module.Invocations["initialize"].Should().ContainSingle().Subject;
		init.Arguments[0].Should().Be("zb-canvas");
		init.Arguments[1].Should().BeSameAs(cut.Instance.Value);
		init.Arguments[2].Should().BeSameAs(cut.Instance.Options);
		init.Arguments[3].Should().BeOfType<DotNetObjectReference<PDZoomBar>>();
	}

	/// <summary>A value reported from JavaScript replaces the current value and is raised.</summary>
	[Fact]
	public async Task OnValueChanged_FromJavaScript_ReplacesValueAndRaises()
	{
		var cut = RenderZoomBar(100);
		var reported = new ZoombarValue { Zoom = 30, Pan = 12.5 };

		await cut.InvokeAsync(() => cut.Instance.OnValueChanged(reported));

		cut.Instance.Value.Should().BeSameAs(reported);
		_changes.Should().Equal(30);
	}

	/// <summary>When the module cannot be imported (strict interop with nothing planned throws) the buttons still step the value without any module call.</summary>
	[Fact]
	public void ImportFailure_IsSwallowed_AndZoomStillWorks()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var cut = RenderZoomBar(100);

		cut.FindAll("button")[1].Click();
		cut.FindAll("button")[0].Click();

		_changes.Should().Equal(90, 100);
	}

	/// <summary>Disposing the component tells the module to release the canvas.</summary>
	[Fact]
	public async Task Dispose_TellsTheModuleToReleaseTheCanvas()
	{
		var module = JSInterop.SetupModule(ModulePath);
		RenderZoomBar(100);

		await DisposeComponentsAsync();

		module.Invocations["dispose"].Should().ContainSingle()
			.Which.Arguments[0].Should().Be("zb-canvas");
	}

	/// <summary>A failure while disposing the module is swallowed rather than surfacing from disposal.</summary>
	[Fact]
	public async Task Dispose_WhenTheModuleThrows_DoesNotThrow()
	{
		var module = JSInterop.SetupModule(ModulePath);
		module.SetupVoid("dispose", _ => true).SetException(new JSException("gone"));
		var cut = RenderZoomBar(100);

		var act = async () => await cut.Instance.DisposeAsync();

		await act.Should().NotThrowAsync();
	}

	/// <summary>Disposing a component whose module never loaded does nothing.</summary>
	[Fact]
	public async Task Dispose_WithoutAModule_DoesNotThrow()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var cut = RenderZoomBar(100);

		var act = async () => await cut.Instance.DisposeAsync();

		await act.Should().NotThrowAsync();
	}
}
