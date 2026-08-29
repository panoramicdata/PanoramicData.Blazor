using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Blazor.Demo.Services;
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
		// One DumbChatService instance seen through two registrations rather than two instances: the
		// conversation history reads the transcripts the chat service holds, so resolving a second
		// DumbChatService for it would give the sidebar a store nothing was ever written to.
		//
		// This has to match PanoramicData.Blazor.Web/Program.cs. The published GitHub Pages demo is built
		// from THIS project, so a service registered only in the Web host is one the public demo does not
		// have - which is how the conversation sidebar came to be invisible on the published site while
		// working locally.
		builder.Services.AddSingleton<DumbChatService>();
		builder.Services.AddSingleton<IChatService>(sp => sp.GetRequiredService<DumbChatService>());
		builder.Services.AddSingleton<IChatConversationService, DemoChatConversationService>();

		await builder.Build().RunAsync().ConfigureAwait(true);
	}
}
