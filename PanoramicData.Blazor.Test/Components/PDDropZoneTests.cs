using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDDropZone"/> initialises its JavaScript uploader and turns the uploader's callbacks
/// into the matching event callbacks.
/// </summary>
/// <remarks>
/// The callbacks are the <c>[JSInvokable]</c> methods the JavaScript module calls, so the tests call them
/// directly exactly as the module would.
/// </remarks>
public partial class PDDropZoneTests : BunitContext
{
	private const string ModulePath = "./_content/PanoramicData.Blazor/PDDropZone.razor.js";

	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up the rendering context and the drop zone's JavaScript module.</summary>
	public PDDropZoneTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		_module = JSInterop.SetupModule(ModulePath);
	}

	/// <summary>The zone renders its id, CSS class and child content, generating an id when none is given.</summary>
	[Fact]
	public void Renders_id_css_and_child_content()
	{
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.CssClass, "extra")
			.Add(p => p.ChildContent, (RenderFragment)(b => b.AddContent(0, "Drop here"))));

		var zone = component.Find("div.pddropzone");
		zone.Id.Should().StartWith("pddz");
		zone.ClassList.Should().Contain("extra");
		zone.TextContent.Trim().Should().Be("Drop here");

		var named = Render<PDDropZone>(parameters => parameters.Add(p => p.Id, "mine"));
		named.Find("div.pddropzone").Id.Should().Be("mine");
	}

	/// <summary>With an upload URL the module is initialised against the zone with the configured options.</summary>
	[Fact]
	public void An_upload_url_initialises_the_uploader()
	{
		Render<PDDropZone>(parameters => parameters
			.Add(p => p.Id, "zone")
			.Add(p => p.UploadUrl, "/upload")
			.Add(p => p.SessionId, "session-1")
			.Add(p => p.Timeout, 5));

		var initialize = _module.VerifyInvoke("initialize");
		initialize.Arguments[0].Should().Be("#zone");
		initialize.Arguments[2].Should().Be("session-1");
		initialize.Arguments[1]!.ToString().Should().Contain("url = /upload").And.Contain("timeout = 5000");
	}

	/// <summary>Without an upload URL the module is never loaded.</summary>
	[Fact]
	public void No_upload_url_does_not_initialise_the_uploader()
	{
		Render<PDDropZone>();

		_module.Invocations["initialize"].Should().BeEmpty();
	}

	/// <summary>A key press is passed on through KeyDown.</summary>
	[Fact]
	public void Key_down_is_raised()
	{
		string? key = null;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.KeyDown, (KeyboardEventArgs a) => key = a.Key));

		component.Find("div.pddropzone").KeyDown(new KeyboardEventArgs { Key = "Delete" });

		key.Should().Be("Delete");
	}

	/// <summary>A drop raises Drop, and whatever the handler sets is returned to the uploader.</summary>
	[Fact]
	public async Task OnDrop_raises_Drop_and_returns_the_handler_decision()
	{
		DropZoneEventArgs? received = null;
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.Drop, (DropZoneEventArgs a) =>
			{
				received = a;
				a.Cancel = true;
				a.CancelReason = "no";
				a.BaseFolder = "/root";
			}));
		var files = new[] { new DropZoneFile { Name = "a.txt", Path = "/" } };

		var result = await component.InvokeAsync(() => component.Instance.OnDrop(files));

		received!.Files.Should().BeSameAs(files);
		received.Sender.Should().BeSameAs(component.Instance);
		var text = result.ToString();
		text.Should().Contain("cancel = True").And.Contain("reason = no").And.Contain("rootDir = /root");
	}

	/// <summary>
	/// CancelAsync and ClearAsync pass the module a CSS selector for the zone, which is what its
	/// <c>document.querySelector</c> needs; the bare id would look for an element named after it (#184).
	/// </summary>
	[Fact]
	public async Task Cancel_and_clear_select_the_zone_by_id()
	{
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.Id, "zone")
			.Add(p => p.UploadUrl, "/upload"));

		await component.InvokeAsync(() => component.Instance.CancelAsync());
		await component.InvokeAsync(() => component.Instance.ClearAsync());

		_module.VerifyInvoke("cancel").Arguments.Should().Equal("#zone");
		_module.VerifyInvoke("clear").Arguments.Should().Equal("#zone");
	}

	/// <summary>
	/// Disposing destroys the uploader in the module. The module's <c>dispose</c> looks the zone up with
	/// <c>document.getElementById</c>, so it is given the bare id, unlike the selector-based functions.
	/// </summary>
	[Fact]
	public async Task Dispose_destroys_the_uploader()
	{
		var component = Render<PDDropZone>(parameters => parameters
			.Add(p => p.Id, "zone")
			.Add(p => p.UploadUrl, "/upload"));

		await component.Instance.DisposeAsync();

		_module.VerifyInvoke("dispose").Arguments.Should().Equal("zone");
	}

	private static DropZoneFile File(string name) => new()
	{
		Name = name,
		Path = "/",
		Size = 42,
		Key = $"k-{name}",
		SessionId = "s"
	};
}
