using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.WebAssembly.Client
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.RootComponents.Add<Demo.App>("#app");

			builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
			builder.Services.AddBlockOverlay();
			builder.Services.AddGlobalEventService();

			await builder.Build().RunAsync().ConfigureAwait(true);
		}
	}
}
