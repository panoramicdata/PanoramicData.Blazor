using AwesomeAssertions;
using Bunit;
using Microsoft.JSInterop;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Services;

/// <summary>Tests for <see cref="NavigationCancelService"/>.</summary>
public class NavigationCancelServiceTests : BunitContext
{
	private readonly BunitJSModuleInterop _module;

	/// <summary>Sets up JavaScript interop with the common module the service imports.</summary>
	public NavigationCancelServiceTests()
	{
		JSInterop.Mode = JSRuntimeMode.Strict;
		_module = JSInterop.SetupModule(JSInteropVersionHelper.CommonJsUrl);
	}

	/// <summary>With no listener objecting, navigation proceeds without asking the user.</summary>
	[Fact]
	public async Task ProceedAsync_NotCancelled_ReturnsTrue()
	{
		var service = new NavigationCancelService(JSInterop.JSRuntime);
		string? target = null;
		service.BeforeNavigate += (_, args) => target = args.Target;

		(await service.ProceedAsync("/next")).Should().BeTrue();

		target.Should().Be("/next");
		_module.Invocations.Should().BeEmpty();
	}

	/// <summary>With no listeners at all, navigation proceeds to an empty target.</summary>
	[Fact]
	public async Task ProceedAsync_NoListeners_ReturnsTrue()
	{
		var service = new NavigationCancelService(JSInterop.JSRuntime);

		(await service.ProceedAsync()).Should().BeTrue();
	}

	/// <summary>When a listener cancels, the user is asked to confirm and their answer decides.</summary>
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ProceedAsync_Cancelled_AsksUser(bool userConfirms)
	{
		_module.Setup<bool>("confirm", "Changes have been made, continue and lose those changes?").SetResult(userConfirms);
		var service = new NavigationCancelService(JSInterop.JSRuntime);
		service.BeforeNavigate += (_, args) => args.Cancel = true;

		var result = await service.ProceedAsync();

		result.Should().Be(userConfirms);
		_module.VerifyInvoke("confirm");
	}

	/// <summary>Constructing the service must not touch JS interop, which is unavailable while prerendering.</summary>
	[Fact]
	public void Constructor_DoesNotInvokeJavaScript()
	{
		_ = new NavigationCancelService(JSInterop.JSRuntime);

		JSInterop.Invocations.Should().BeEmpty();
	}

	/// <summary>The module is imported once, on first use, and reused for later confirmations.</summary>
	[Fact]
	public async Task ProceedAsync_CancelledTwice_ImportsModuleOnce()
	{
		_module.Setup<bool>("confirm", "Changes have been made, continue and lose those changes?").SetResult(true);
		var service = new NavigationCancelService(JSInterop.JSRuntime);
		service.BeforeNavigate += (_, args) => args.Cancel = true;

		await service.ProceedAsync();
		await service.ProceedAsync();

		JSInterop.Invocations.Count(i => i.Identifier == "import").Should().Be(1);
		_module.Invocations.Count(i => i.Identifier == "confirm").Should().Be(2);
	}
}
