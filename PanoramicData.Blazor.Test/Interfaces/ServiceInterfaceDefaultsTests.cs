using System;
using AwesomeAssertions;
using PanoramicData.Blazor.Arguments;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace PanoramicData.Blazor.Test.Interfaces;

/// <summary>
/// Tests for the parameterless overloads that the service interfaces supply by default, as seen by an
/// implementation that provides only the full-argument member.
/// </summary>
public class ServiceInterfaceDefaultsTests
{
	/// <summary>Showing the block overlay without content shows it with no HTML.</summary>
	[Fact]
	public void BlockOverlay_Show_ShowsWithoutContent()
	{
		var overlay = new MinimalBlockOverlayService();
		var shown = new List<string?>();
		overlay.OnShow += shown.Add;

		((IBlockOverlayService)overlay).Show();

		shown.Should().ContainSingle().Which.Should().BeNull();
	}

	/// <summary>Asking to proceed without a target asks the listeners about an empty target.</summary>
	[Fact]
	public async Task NavigationCancel_Proceed_AsksAboutAnEmptyTarget()
	{
		var navigation = new MinimalNavigationCancelService();
		var targets = new List<string>();
		navigation.BeforeNavigate += (_, args) => targets.Add(args.Target);

		var proceed = await ((INavigationCancelService)navigation).ProceedAsync();

		proceed.Should().BeTrue();
		targets.Should().Equal(string.Empty);
	}

	/// <summary>Asking for a basic preview without saying otherwise asks for one without a spinner.</summary>
	[Fact]
	public async Task Preview_GetBasicPreviewInfo_AsksForNoSpinner()
	{
		var provider = new MinimalPreviewProvider();

		await ((IPreviewProvider)provider).GetBasicPreviewInfoAsync(null);

		provider.SpinnerRequests.Should().Equal(false);
	}

	private sealed class MinimalBlockOverlayService : IBlockOverlayService
	{
		public event Action? OnHide;

		public event Action<string?>? OnShow;

		public void Hide() => OnHide?.Invoke();

		public void Show(string? html) => OnShow?.Invoke(html);
	}

	private sealed class MinimalNavigationCancelService : INavigationCancelService
	{
		public event EventHandler<BeforeNavigateEventArgs> BeforeNavigate = (_, _) => { };

		public Task<bool> ProceedAsync(string target)
		{
			var args = new BeforeNavigateEventArgs { Target = target };
			BeforeNavigate.Invoke(this, args);
			return Task.FromResult(!args.Cancel);
		}
	}

	private sealed class MinimalPreviewProvider : IPreviewProvider
	{
		public List<bool> SpinnerRequests { get; } = [];

		public string DateTimeFormat { get; set; } = "yyyy-MM-dd";

		public int SpinnerTriggerMs { get; set; }

		public int SpinnerMinDisplayMs { get; set; }

		public Task<PreviewInfo> GetBasicPreviewInfoAsync(FileExplorerItem? item, bool spinner)
		{
			SpinnerRequests.Add(spinner);
			return Task.FromResult(new PreviewInfo { CssClass = item?.Name ?? string.Empty });
		}

		public Task<PreviewInfo> GetPreviewInfoAsync(FileExplorerItem? item) => GetBasicPreviewInfoAsync(item, true);
	}
}
