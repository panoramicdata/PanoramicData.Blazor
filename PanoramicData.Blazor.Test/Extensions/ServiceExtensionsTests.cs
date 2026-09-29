using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Test.Extensions;

/// <summary>Tests for <see cref="ServiceExtensions"/>.</summary>
public class ServiceExtensionsTests
{
	private static void ShouldRegisterScoped<TService, TImplementation>(IServiceCollection services)
	{
		services.Should().ContainSingle(d => d.ServiceType == typeof(TService))
			.Which.Should().Match<ServiceDescriptor>(d => d.ImplementationType == typeof(TImplementation) && d.Lifetime == ServiceLifetime.Scoped);
	}

	/// <summary>Each individual registration method adds one scoped service and returns the collection for chaining.</summary>
	[Fact]
	public void IndividualRegistrations_AddScopedServices()
	{
		var services = new ServiceCollection();

		services.AddBlockOverlay().Should().BeSameAs(services);
		services.AddGlobalEventService().Should().BeSameAs(services);
		services.AddNavigationCancelService().Should().BeSameAs(services);
		services.AddListenerService().Should().BeSameAs(services);

		ShouldRegisterScoped<IBlockOverlayService, BlockOverlayService>(services);
		ShouldRegisterScoped<IGlobalEventService, GlobalEventService>(services);
		ShouldRegisterScoped<INavigationCancelService, NavigationCancelService>(services);
		ShouldRegisterScoped<IListenerService, ListenerService>(services);
	}

	/// <summary>The all-in-one registration adds all four services.</summary>
	[Fact]
	public void AddPanoramicDataBlazor_AddsAllServices()
	{
		var services = new ServiceCollection();

		services.AddPanoramicDataBlazor().Should().BeSameAs(services);

		services.Should().HaveCount(4);
		ShouldRegisterScoped<IBlockOverlayService, BlockOverlayService>(services);
		ShouldRegisterScoped<IListenerService, ListenerService>(services);
	}

	/// <summary>The services that need nothing from the host can be resolved.</summary>
	[Fact]
	public void RegisteredServices_CanBeResolved()
	{
		using var provider = new ServiceCollection().AddBlockOverlay().AddGlobalEventService().AddListenerService().BuildServiceProvider();
		using var scope = provider.CreateScope();

		scope.ServiceProvider.GetRequiredService<IBlockOverlayService>().Should().BeOfType<BlockOverlayService>();
		scope.ServiceProvider.GetRequiredService<IGlobalEventService>().Should().BeOfType<GlobalEventService>();
		scope.ServiceProvider.GetRequiredService<IListenerService>().Should().BeOfType<ListenerService>();
	}
}
