using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Services;

namespace PanoramicData.Blazor.Extensions
{
	public static class ServiceExtensions
	{
		/// <summary>
		/// Add the BlockOverlay service to allow injection of the IBlockOverlayService
		/// </summary>
		/// <param name="services"></param>
		/// <returns>The IServiceCollection for further adds</returns>
		public static IServiceCollection AddBlockOverlay(this IServiceCollection services)
			=> services.AddScoped<IBlockOverlayService, BlockOverlayService>();
	}
}