namespace PanoramicData.Blazor.WebAssembly.Server;

/// <summary>
/// Configures services and the HTTP request pipeline for the Blazor WebAssembly server host.
/// </summary>
/// <param name="configuration">The application configuration.</param>
public class Startup(IConfiguration configuration)
{
	/// <summary>Gets the application configuration.</summary>
	public IConfiguration Configuration { get; } = configuration;

	// This method gets called by the runtime. Use this method to add services to the container.
	// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
	/// <summary>
	/// Registers application services with the dependency injection container.
	/// </summary>
	/// <param name="services">The service collection to configure.</param>
	public void ConfigureServices(IServiceCollection services)
	{

		services.AddControllersWithViews();
		services.AddRazorPages();
	}

	/// <summary>
	/// Configures the HTTP request pipeline.
	/// </summary>
	/// <param name="app">The application builder.</param>
	/// <param name="env">The hosting environment.</param>
	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
			app.UseWebAssemblyDebugging();
		}
		else
		{
			app.UseExceptionHandler("/Error");
		}

		app.UseBlazorFrameworkFiles();
		app.UseStaticFiles();

		app.UseRouting();

		app.UseEndpoints(endpoints =>
		{
			endpoints.MapRazorPages();
			endpoints.MapControllers();
			endpoints.MapFallbackToFile("index.html");
		});
	}
}
