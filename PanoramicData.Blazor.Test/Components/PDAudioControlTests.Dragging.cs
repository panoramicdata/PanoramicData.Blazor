using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Pointer drag tests for the <see cref="PDAudioControl"/> base class.
/// </summary>
public partial class PDAudioControlTests
{
	private IRenderedComponent<TestAudioControl> RenderDraggable(Action<ComponentParameterCollectionBuilder<TestAudioControl>>? configure = null)
	{
		JSInterop.SetupModule(AudioModulePath);
		return RenderControl(p =>
		{
			p.Add(x => x.ModulePath, AudioModulePath);
			configure?.Invoke(p);
		});
	}

	private static void PointerDownOn(IRenderedComponent<TestAudioControl> cut)
		=> cut.Find("div.audio").PointerDown(new PointerEventArgs { ClientY = 100 });

	private static Task MoveAsync(IRenderedComponent<TestAudioControl> cut, int y)
		=> cut.InvokeAsync(() => cut.Instance.OnPointerMove(y));

	/// <summary>A pointer down registers drag events with the module, passing a .NET reference.</summary>
	[Fact]
	public void PointerDown_RegistersDragEventsWithTheModule()
	{
		var module = JSInterop.SetupModule(AudioModulePath);
		var cut = RenderControl(p => p.Add(x => x.ModulePath, AudioModulePath));

		PointerDownOn(cut);

		module.Invocations["registerAudioControlEvents"].Should().ContainSingle()
			.Which.Arguments[0].Should().BeOfType<DotNetObjectReference<PDAudioControl>>();
	}

	/// <summary>A second pointer down during a drag does not register again.</summary>
	[Fact]
	public void PointerDown_WhileDragging_DoesNotRegisterAgain()
	{
		var module = JSInterop.SetupModule(AudioModulePath);
		var cut = RenderControl(p => p.Add(x => x.ModulePath, AudioModulePath));

		PointerDownOn(cut);
		PointerDownOn(cut);

		module.Invocations["registerAudioControlEvents"].Should().ContainSingle();
	}

	/// <summary>A disabled control ignores pointer down.</summary>
	[Fact]
	public void PointerDown_WhenDisabled_DoesNothing()
	{
		var module = JSInterop.SetupModule(AudioModulePath);
		var cut = RenderControl(p => p.Add(x => x.ModulePath, AudioModulePath).Add(x => x.IsEnabled, false));

		PointerDownOn(cut);

		module.Invocations.Should().BeEmpty();
	}

	/// <summary>A control without a module file does not start a drag, so pointer moves change nothing.</summary>
	[Fact]
	public async Task PointerDown_WithoutModule_DoesNotStartADrag()
	{
		var cut = RenderControl();

		PointerDownOn(cut);
		await MoveAsync(cut, 0);

		_values.Should().BeEmpty();
	}

	/// <summary>Moving the pointer up during a drag raises the value by the distance over 150 pixels, clamped to 1.</summary>
	[Fact]
	public async Task PointerMove_DuringDrag_ChangesValueByDistance()
	{
		var cut = RenderDraggable(p => p.Add(x => x.Value, 0.2));

		PointerDownOn(cut);
		await MoveAsync(cut, 70);
		await MoveAsync(cut, -500);

		_values.Should().HaveCount(2);
		_values[0].Should().BeApproximately(0.4, 1e-9);
		_values[1].Should().Be(1);
	}

	/// <summary>During a drag with snapping, the value is quantised to the snap grid.</summary>
	[Fact]
	public async Task PointerMove_WithSnapping_QuantisesTheValue()
	{
		var cut = RenderDraggable(p => p.Add(x => x.Value, 0).Add(x => x.SnapPoints, 5));

		PointerDownOn(cut);
		await MoveAsync(cut, 80);

		_values.Should().Equal(0.25);
	}

	/// <summary>A pointer move that leaves the value unchanged reports nothing.</summary>
	[Fact]
	public async Task PointerMove_WithNoChange_ReportsNothing()
	{
		var cut = RenderDraggable(p => p.Add(x => x.Value, 0.5));

		PointerDownOn(cut);
		await MoveAsync(cut, 100);

		_values.Should().BeEmpty();
	}

	/// <summary>After pointer up, further pointer moves are ignored.</summary>
	[Fact]
	public async Task PointerUp_EndsTheDrag()
	{
		var cut = RenderDraggable();

		PointerDownOn(cut);
		cut.Instance.OnPointerUp(100);
		await MoveAsync(cut, 0);

		_values.Should().BeEmpty();
	}

	/// <summary>Disposing after a drag was started releases the module without error.</summary>
	[Fact]
	public async Task Dispose_AfterDrag_DoesNotThrow()
	{
		var cut = RenderDraggable();
		PointerDownOn(cut);

		var act = async () => await cut.Instance.DisposeAsync();

		await act.Should().NotThrowAsync();
	}
}
