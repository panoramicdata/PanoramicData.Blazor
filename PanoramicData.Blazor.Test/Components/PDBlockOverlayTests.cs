using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDBlockOverlay"/> follows the show and hide requests raised through
/// <see cref="IBlockOverlayService"/>, and lets go of the service when it is disposed.
/// </summary>
public class PDBlockOverlayTests : BunitContext
{
	private readonly CountingOverlayService _service = new();

	/// <summary>Sets up the rendering context with a service that counts its subscribers.</summary>
	public PDBlockOverlayTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddSingleton<IBlockOverlayService>(_service);
	}

	/// <summary>Before anything is requested the overlay is hidden and has no text box.</summary>
	[Fact]
	public void InitialRender_IsHidden_WithNoTextBox()
	{
		var cut = Render<PDBlockOverlay>();

		cut.Find("div").ClassList.Should().Contain("blockoverlay_hide");
		cut.FindAll(".text-box").Should().BeEmpty();
	}

	/// <summary>A show request through the service makes the overlay visible and renders the supplied HTML as markup.</summary>
	[Fact]
	public void ServiceShow_WithHtml_ShowsOverlayAndRendersMarkup()
	{
		var cut = Render<PDBlockOverlay>();

		cut.InvokeAsync(() => _service.Show("<b>Please wait</b>"));

		cut.Find("div").ClassList.Should().Contain("blockoverlay_show");
		cut.Find(".text-box b").TextContent.Should().Be("Please wait");
	}

	/// <summary>A show request without HTML makes the overlay visible without a text box.</summary>
	[Fact]
	public void ServiceShow_WithoutHtml_ShowsOverlayWithoutTextBox()
	{
		var cut = Render<PDBlockOverlay>();

		cut.InvokeAsync(() => _service.Show(null));

		cut.Find("div").ClassList.Should().Contain("blockoverlay_show");
		cut.FindAll(".text-box").Should().BeEmpty();
	}

	/// <summary>The parameterless public <see cref="PDBlockOverlay.Show()"/> shows the overlay with no content.</summary>
	[Fact]
	public void PublicShow_Parameterless_ShowsOverlay()
	{
		var cut = Render<PDBlockOverlay>();

		cut.InvokeAsync(() => cut.Instance.Show());

		cut.Find("div").ClassList.Should().Contain("blockoverlay_show");
		cut.FindAll(".text-box").Should().BeEmpty();
	}

	/// <summary>A hide request after a show hides the overlay again and removes the shown text.</summary>
	[Fact]
	public void ServiceHide_AfterShow_HidesOverlayAndClearsText()
	{
		var cut = Render<PDBlockOverlay>();
		cut.InvokeAsync(() => _service.Show("Busy"));

		cut.InvokeAsync(_service.Hide);

		cut.Find("div").ClassList.Should().Contain("blockoverlay_hide");
		cut.Markup.Should().NotContain("Busy");
	}

	/// <summary>A right-click on the overlay is absorbed without changing its state.</summary>
	[Fact]
	public void ContextMenu_IsAbsorbed_WithoutChangingState()
	{
		var cut = Render<PDBlockOverlay>();
		cut.InvokeAsync(() => _service.Show("Busy"));

		cut.Find("div").ContextMenu();

		cut.Find("div").ClassList.Should().Contain("blockoverlay_show");
		cut.Find(".text-box").TextContent.Trim().Should().Be("Busy");
	}

	/// <summary>The component subscribes to both service events on initialisation and unsubscribes both on disposal.</summary>
	[Fact]
	public void Dispose_UnsubscribesFromBothServiceEvents()
	{
		var cut = Render<PDBlockOverlay>();
		_service.ShowSubscribers.Should().Be(1);
		_service.HideSubscribers.Should().Be(1);

		cut.Instance.Dispose();

		_service.ShowSubscribers.Should().Be(0);
		_service.HideSubscribers.Should().Be(0);
	}

	/// <summary>An overlay service that counts how many handlers are attached to each event.</summary>
	private sealed class CountingOverlayService : IBlockOverlayService
	{
		private Action? _onHide;
		private Action<string?>? _onShow;

		public int ShowSubscribers => _onShow?.GetInvocationList().Length ?? 0;

		public int HideSubscribers => _onHide?.GetInvocationList().Length ?? 0;

		public event Action? OnHide
		{
			add => _onHide += value;
			remove => _onHide -= value;
		}

		public event Action<string?>? OnShow
		{
			add => _onShow += value;
			remove => _onShow -= value;
		}

		public void Hide() => _onHide?.Invoke();

		public void Show(string? html = null) => _onShow?.Invoke(html);
	}
}
