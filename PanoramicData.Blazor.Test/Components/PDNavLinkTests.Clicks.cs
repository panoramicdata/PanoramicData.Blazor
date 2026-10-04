using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components.Web;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Click, new-tab and disposal tests for <see cref="PDNavLink"/>.
/// </summary>
public partial class PDNavLinkTests
{
	/// <summary>Clicking navigates when the cancel service allows it.</summary>
	[Fact]
	public void Click_WhenAllowed_Navigates()
	{
		var component = RenderLink("counter");

		component.Find("a").Click();

		_cancelService.Targets.Should().Equal("http://localhost/counter");
		Navigation.Uri.Should().Be("http://localhost/counter");
	}

	/// <summary>Clicking does not navigate when the cancel service refuses.</summary>
	[Fact]
	public void Click_WhenCancelled_DoesNotNavigate()
	{
		_cancelService.Proceed = false;
		var component = RenderLink("counter");

		component.Find("a").Click();

		Navigation.Uri.Should().Be("http://localhost/");
	}

	/// <summary>A ctrl-click opens the link in a new tab through JS, without asking or navigating.</summary>
	[Fact]
	public void CtrlClick_OpensANewTab()
	{
		var module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
		module.SetupVoid("openUrl", "http://localhost/counter", "_blank").SetVoidResult();
		var component = RenderLink("counter");

		component.Find("a").Click(new MouseEventArgs { CtrlKey = true });

		module.VerifyInvoke("openUrl");
		_cancelService.Targets.Should().BeEmpty();
		Navigation.Uri.Should().Be("http://localhost/");
	}

	/// <summary>When the JS module could not load, a ctrl-click falls back to an ordinary navigation.</summary>
	[Fact]
	public void CtrlClick_WithoutTheModule_NavigatesNormally()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		var component = RenderLink("counter");

		component.Find("a").Click(new MouseEventArgs { CtrlKey = true });

		Navigation.Uri.Should().Be("http://localhost/counter");
	}

	/// <summary>After disposal the link stops following location changes.</summary>
	[Fact]
	public async Task Dispose_StopsFollowingLocationChanges()
	{
		var component = RenderLink("counter");

		await component.InvokeAsync(() => component.Instance.DisposeAsync().AsTask());
		Navigation.NavigateTo("counter");

		component.Find("a").GetAttribute("class").Should().Be("nav-link");
	}
}
