using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.WebAssembly.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PanoramicData.Blazor.WebAssembly.Client;

public static class Program
{
	public static async Task Main(string[] args)
	{
		var builder = WebAssemblyHostBuilder.CreateDefault(args);
		builder.RootComponents.Add<App>("#app");

		builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
		builder.Services.AddPanoramicDataBlazor();
		builder.Services.AddSingleton<IChatService, DumbChatService>(); // Register the dumb chat service for demonstration purposes

		await builder.Build().RunAsync().ConfigureAwait(true);
	}
}
