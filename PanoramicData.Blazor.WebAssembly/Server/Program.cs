namespace PanoramicData.Blazor.WebAssembly.Server;

/// <summary>
/// Entry point for the Blazor WebAssembly server-side host.
/// </summary>
public static class Program
{
	/// <summary>
	/// Builds and runs the host.
	/// </summary>
	/// <param name="args">Command-line arguments.</param>
	public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

	/// <summary>
	/// Creates the default <see cref="IHostBuilder"/> configured to use <see cref="Startup"/>.
	/// </summary>
	/// <param name="args">Command-line arguments.</param>
	/// <returns>A configured <see cref="IHostBuilder"/>.</returns>
	public static IHostBuilder CreateHostBuilder(string[] args) =>
		Host.CreateDefaultBuilder(args)
			.ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
