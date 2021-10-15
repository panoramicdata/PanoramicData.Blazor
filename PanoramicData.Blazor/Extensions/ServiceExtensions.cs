using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Extensions
{
	public static class ServiceExtensions
	{
		/// <summary>
		/// Add the BlockOverlay service to allow injection of the IBlockOverlayService
		/// </summary>
		/// <param name="services">Service collection to add service to.</param>
		/// <returns>The IServiceCollection for further adds</returns>
		public static IServiceCollection AddBlockOverlay(this IServiceCollection services)
			=> services.AddScoped<IBlockOverlayService, BlockOverlayService>();

		/// <summary>
		/// Add the GlobalEventService service to allow injection of the IGlobalEventService
		/// </summary>
		/// <param name="services">Service collection to add service to.</param>
		/// <returns>The IServiceCollection for further adds</returns>
		public static IServiceCollection AddGlobalEventService(this IServiceCollection services)
			=> services.AddScoped<IGlobalEventService, GlobalEventService>();

		/// <summary>
		/// Add the NavigationCancelService service to allow injection of the INavigationCancelService
		/// </summary>
		/// <param name="services">Service collection to add service to.</param>
		/// <returns>The IServiceCollection for further adds</returns>
		public static IServiceCollection AddNavigationCancelService(this IServiceCollection services)
			=> services.AddScoped<INavigationCancelService, NavigationCancelService>();

		/// <summary>
		/// Adds all the required services from the PanoramicData.Blazor library.
		/// </summary>
		/// <param name="services">Service collection to add service to.</param>
		/// <returns>The IServiceCollection for further adds</returns>
		public static IServiceCollection AddPanoramicDataBlazor(this IServiceCollection services)
		{
			services.AddScoped<IBlockOverlayService, BlockOverlayService>();
			services.AddScoped<IGlobalEventService, GlobalEventService>();
			services.AddScoped<INavigationCancelService, NavigationCancelService>();
			return services;
		}
	}
}