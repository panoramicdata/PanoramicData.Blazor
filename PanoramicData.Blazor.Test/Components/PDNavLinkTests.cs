using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;

namespace PanoramicData.Blazor.Test.Components;

/// <summary>
/// Tests that <see cref="PDNavLink"/> shows its active state for the current location and navigates only
/// when the navigation cancel service allows it.
/// </summary>
public class PDNavLinkTests : BunitContext
{
	private readonly FakeNavigationCancelService _cancelService = new();

	/// <summary>Sets up the rendering context.</summary>
	public PDNavLinkTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddPanoramicDataBlazor();
		Services.AddScoped<INavigationCancelService>(_ => _cancelService);
	}

	private NavigationManager Navigation => Services.GetRequiredService<NavigationManager>();

	private IRenderedComponent<PDNavLink> RenderLink(string? href, NavLinkMatch match = NavLinkMatch.All, string? activeClass = null)
		=> Render<PDNavLink>(parameters =>
		{
			parameters.Add(p => p.Match, match);
			parameters.AddUnmatched("class", "nav-link");
			if (href != null)
			{
				parameters.AddUnmatched("href", href);
			}

			if (activeClass != null)
			{
				parameters.Add(p => p.ActiveClass, activeClass);
			}

			parameters.Add(p => p.ChildContent, (RenderFragment)(b => b.AddContent(0, "Go")));
		});

	/// <summary>A link to somewhere else is not active and keeps its own class and content.</summary>
	[Fact]
	public void LinkElsewhere_IsNotActive()
	{
		var component = RenderLink("counter");

		var anchor = component.Find("a");
		anchor.GetAttribute("class").Should().Be("nav-link");
		anchor.GetAttribute("href").Should().Be("counter");
		anchor.TextContent.Should().Be("Go");
	}

	/// <summary>Navigating to the link's target makes it active, and away again makes it inactive.</summary>
	[Fact]
	public void NavigatingToTheTarget_MakesTheLinkActive()
	{
		var component = RenderLink("counter");

		Navigation.NavigateTo("counter");
		component.Find("a").GetAttribute("class").Should().Be("nav-link active");

		Navigation.NavigateTo("other");
		component.Find("a").GetAttribute("class").Should().Be("nav-link");
	}

	/// <summary>A custom active class is used in place of the default.</summary>
	[Fact]
	public void ActiveClass_ReplacesTheDefault()
	{
		Navigation.NavigateTo("counter");

		var component = RenderLink("counter", activeClass: "is-current");

		component.Find("a").GetAttribute("class").Should().Be("nav-link is-current");
	}

	/// <summary>With no class attribute an active link has just the active class.</summary>
	[Fact]
	public void ActiveWithoutAClass_HasOnlyTheActiveClass()
	{
		Navigation.NavigateTo("counter");

		var component = Render<PDNavLink>(parameters => parameters.AddUnmatched("href", "counter"));

		component.Find("a").GetAttribute("class").Should().Be("active");
	}

	/// <summary>Prefix matching activates the link for child paths, but only across a path separator.</summary>
	[Theory]
	[InlineData("docs/intro", NavLinkMatch.Prefix, true)]
	[InlineData("docs/intro", NavLinkMatch.All, false)]
	[InlineData("docsx", NavLinkMatch.Prefix, false)]
	[InlineData("doc", NavLinkMatch.Prefix, false)]
	public void PrefixMatching_RespectsPathSeparators(string location, NavLinkMatch match, bool expectActive)
	{
		Navigation.NavigateTo(location);

		var component = RenderLink("docs", match);

		component.Find("a").ClassList.Contains("active").Should().Be(expectActive);
	}

	/// <summary>A prefix ending in a separator matches a child path.</summary>
	[Fact]
	public void PrefixEndingInASeparator_MatchesAChildPath()
	{
		Navigation.NavigateTo("docs/intro");

		var component = RenderLink("docs/", NavLinkMatch.Prefix);

		component.Find("a").ClassList.Should().Contain("active");
	}

	/// <summary>A link with a trailing slash is active at the same path without one.</summary>
	[Fact]
	public void TrailingSlashLink_IsActiveWithoutTheSlash()
	{
		Navigation.NavigateTo("docs");

		var component = RenderLink("docs/");

		component.Find("a").ClassList.Should().Contain("active");
	}

	/// <summary>A link without an href is never active.</summary>
	[Fact]
	public void LinkWithoutHref_IsNeverActive()
	{
		var component = RenderLink(null);

		Navigation.NavigateTo("anything");

		component.Find("a").GetAttribute("class").Should().Be("nav-link");
	}

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

	/// <summary>A cancel service double that records each target and returns a configurable answer.</summary>
	private sealed class FakeNavigationCancelService : INavigationCancelService
	{
		public bool Proceed { get; set; } = true;

		public List<string> Targets { get; } = [];

		private EventHandler<BeforeNavigateEventArgs>? _beforeNavigate;

		public event EventHandler<BeforeNavigateEventArgs> BeforeNavigate
		{
			add => _beforeNavigate += value;
			remove => _beforeNavigate -= value;
		}

		public Task<bool> ProceedAsync(string target = "")
		{
			Targets.Add(target);
			_beforeNavigate?.Invoke(this, new BeforeNavigateEventArgs { Target = target });
			return Task.FromResult(Proceed);
		}
	}
}
