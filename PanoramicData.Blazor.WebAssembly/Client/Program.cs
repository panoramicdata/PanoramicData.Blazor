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

/// <summary>
/// Entry point for the Blazor WebAssembly client application.
/// </summary>
public static class Program
{
	/// <summary>
	/// Configures services and launches the WebAssembly host.
	/// </summary>
	/// <param name="args">Command-line arguments.</param>
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
