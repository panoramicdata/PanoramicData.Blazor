using AwesomeAssertions;
using Bunit;

namespace PanoramicData.Blazor.Test;

/// <summary>
/// Sizing, layout failure and disposal tests for <see cref="PDTreeMap{TItem}"/>.
/// </summary>
public partial class PDTreeMapTests
{
	/// <summary>
	/// Verifies that a layout failure is reported and the empty state shown.
	/// </summary>
	[Fact]
	public void A_layout_failure_is_reported_and_shows_the_empty_state()
	{
		var component = RenderMap(sizeSelector: _ => throw new InvalidOperationException("size"));

		component.Find(".pdtm-empty").Should().NotBeNull();
		_errors.Should().ContainSingle().Which.Message.Should().Be("size");
	}

	/// <summary>
	/// Verifies that fixed dimensions need no JavaScript.
	/// </summary>
	[Fact]
	public void Fixed_dimensions_do_not_load_the_module()
	{
		var module = JSInterop.SetupModule(ModulePath);

		RenderMap();

		module.Invocations["init"].Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that without fixed dimensions the container is observed, and reported sizes lay it out.
	/// </summary>
	[Fact]
	public async Task Without_fixed_dimensions_the_container_size_drives_the_layout()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var component = Render<PDTreeMap<Node>>(parameters => AddHierarchy(parameters));
		component.Find(".pdtm-empty").Should().NotBeNull("nothing can be laid out before the size is known");

		var init = module.VerifyInvoke("init");
		init.Arguments[0].Should().Be(component.Instance.Id);

		await component.InvokeAsync(() => component.Instance.OnContainerResized(300, 150));

		component.Find("svg").GetAttribute("viewBox").Should().Be("0 0 300 150");
		component.Instance.Rectangles.Should().HaveCount(5);
	}

	/// <summary>
	/// Verifies that sub-pixel resizes are ignored and a fixed dimension overrides the reported one.
	/// </summary>
	[Fact]
	public async Task Sub_pixel_resizes_are_ignored_and_a_fixed_width_wins()
	{
		JSInterop.SetupModule(ModulePath);
		var component = Render<PDTreeMap<Node>>(parameters => AddHierarchy(parameters).Add(p => p.Width, 250));

		await component.InvokeAsync(() => component.Instance.OnContainerResized(999, 100));
		component.Find("svg").GetAttribute("viewBox").Should().Be("0 0 250 100");

		await component.InvokeAsync(() => component.Instance.OnContainerResized(999, 100.5));
		component.Find("svg").GetAttribute("viewBox").Should().Be("0 0 250 100");
	}

	/// <summary>
	/// Verifies that disposing stops the container observation.
	/// </summary>
	[Fact]
	public async Task Disposing_stops_observing_the_container()
	{
		var module = JSInterop.SetupModule(ModulePath);
		var component = Render<PDTreeMap<Node>>(parameters => AddHierarchy(parameters));

		await component.Instance.DisposeAsync();

		module.VerifyInvoke("dispose").Arguments.Should().Equal(component.Instance.Id);
	}
}
