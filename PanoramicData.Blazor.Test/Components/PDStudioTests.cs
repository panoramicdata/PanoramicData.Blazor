using AwesomeAssertions;
using Bunit;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests for <see cref="PDStudio"/>: its layout, running code through its service, cancelling, the service's
/// execution events, the menus and keyboard shortcut.
/// </summary>
/// <remarks>
/// The Monaco editor needs a browser, so code is entered by raising the editor's value change the way the
/// editor itself would.
/// </remarks>
public partial class PDStudioTests : BunitContext
{
	/// <summary>Sets up the rendering context.</summary>
	public PDStudioTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;

		// The real Monaco editor cannot start without a browser, and calls into it then fail, so the studio is
		// given an editor that keeps every parameter but renders no Monaco instance.
		ComponentFactories.Add<PDMonacoEditor, HeadlessEditor>();
		Services.AddPanoramicDataBlazor();
	}

	private IRenderedComponent<PDStudio> RenderStudio(
		PDStudioOptions? options = null,
		Action<ComponentParameterCollectionBuilder<PDStudio>>? more = null)
		=> Render<PDStudio>(parameters =>
		{
			parameters
				.Add(p => p.StudioService, _service)
				.Add(p => p.Options, options ?? new PDStudioOptions());
			more?.Invoke(parameters);
		});

	private static Task TypeAsync(IRenderedComponent<PDStudio> studio, string code)
	{
		var editor = studio.FindComponent<PDMonacoEditor>().Instance;
		return studio.InvokeAsync(() => editor.ValueChanged.InvokeAsync(code));
	}

	private static AngleSharp.Dom.IElement ToolbarButton(IRenderedComponent<PDStudio> studio, string key)
		=> studio.FindComponents<PDToolbarButton>().Single(b => b.Instance.Key == key).Find("button.pdtoolbarbutton");

	private static AngleSharp.Dom.IElement PlayButton(IRenderedComponent<PDStudio> studio)
		=> ToolbarButton(studio, "play");

	private static string Status(IRenderedComponent<PDStudio> studio)
		=> studio.Find(".pd-studio-results-status .status-text").TextContent;

	private static List<string> LogMessages(IRenderedComponent<PDStudio> studio)
		=> [.. studio.FindAll(".log-message").Select(m => m.TextContent)];

	private static Task MenuAsync(IRenderedComponent<PDStudio> studio, string menu, string key)
	{
		var dropdown = studio.FindComponents<PDToolbarDropdown>().Single(d => d.Instance.Text == menu).Instance;
		return studio.InvokeAsync(() => dropdown.Click.InvokeAsync(key));
	}

	/// <summary>
	/// Verifies a hidden log leaves a classed placeholder with no inline style, so a host's CSS can restyle it.
	/// </summary>
	[Fact]
	public void Layout_LogHidden_PlaceholderHasNoInlineStyle()
	{
		var studio = RenderStudio(new PDStudioOptions { IsLoggingVisible = false });

		studio.FindAll(".pd-log").Should().BeEmpty();
		studio.Find(".pd-studio-log-hidden").HasAttribute("style").Should().BeFalse();
	}

	/// <summary>
	/// Verifies the default layout: New and Execute but no Examples menu without examples, a ready results panel and a log.
	/// </summary>
	[Fact]
	public void Layout_HasNewExecuteResultsAndLog()
	{
		var studio = RenderStudio(more: p => p.Add(x => x.CssClass, "my-studio"));

		studio.Find(".pd-studio").ClassList.Should().Contain("my-studio");
		studio.FindComponents<PDToolbarButton>().Select(b => b.Instance.Key).Should().Equal("new", "play");
		studio.FindComponents<PDToolbarDropdown>().Should().BeEmpty();
		studio.FindComponents<PDToolbarSeparator>().Should().ContainSingle();
		PlayButton(studio).TextContent.Should().Contain("Execute");
		PlayButton(studio).ClassList.Should().Contain("btn-success");
		Status(studio).Should().Be("Ready");
		studio.Instance.LogComponent.Should().NotBeNull();
	}

	/// <summary>
	/// Verifies that the menu, toolbar and log can each be left out, and custom toolbar content is added after a separator.
	/// </summary>
	[Fact]
	public void Layout_PartsCanBeLeftOutOrAdded()
	{
		var withoutMenu = RenderStudio(
			new PDStudioOptions { ShowMenu = false, IsLoggingVisible = false, ShowStatusBar = false },
			p => p.Add(x => x.EditorToolbarContent, b => b.AddMarkupContent(0, "<span class=\"extra\">Extra</span>")));

		withoutMenu.FindComponents<PDToolbarButton>().Select(b => b.Instance.Key).Should().Equal("play");
		withoutMenu.FindComponents<PDToolbarSeparator>().Should().ContainSingle();
		withoutMenu.Find(".pd-studio-toolbar .extra").TextContent.Should().Be("Extra");
		withoutMenu.FindComponents<PDLog>().Should().BeEmpty();
		withoutMenu.Instance.LogComponent.Should().BeNull();
		withoutMenu.FindAll(".pd-studio-results-status").Should().BeEmpty();

		var withoutToolbar = RenderStudio(new PDStudioOptions { ShowToolbar = false });
		withoutToolbar.FindComponents<PDToolbarButton>().Select(b => b.Instance.Key).Should().Equal("new");

		var neither = RenderStudio(new PDStudioOptions { ShowToolbar = false, ShowMenu = false });
		neither.FindAll(".pd-studio-toolbar").Should().BeEmpty();
	}

	/// <summary>
	/// Verifies that the service's running state and status are picked up when the studio is given its parameters.
	/// </summary>
	[Fact]
	public void ServiceState_IsPickedUpFromTheService()
	{
		_service.IsExecuting = true;
		_service.CurrentStatus = StudioExecutionStatus.Processing;

		var studio = RenderStudio();

		PlayButton(studio).TextContent.Should().Contain("Cancel");
		studio.FindAll(".pd-studio-results-status .spinner-border").Should().ContainSingle();
	}

	/// <summary>
	/// Verifies that after disposal the studio no longer listens to the service.
	/// </summary>
	[Fact]
	public async Task Dispose_StopsListeningToTheService()
	{
		RenderStudio();
		_service.HasListeners.Should().BeTrue();

		await DisposeComponentsAsync();

		_service.HasListeners.Should().BeFalse();
	}

	private sealed class HeadlessEditor : PDMonacoEditor
	{
		protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
			=> builder.AddMarkupContent(0, "<div class=\"headless-editor\"></div>");
	}
}
