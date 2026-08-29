using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PanoramicData.Blazor.Demo.Services;
using PanoramicData.Blazor.Extensions;
using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Services;
using PanoramicData.Blazor.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddRazorComponents()
	.AddInteractiveServerComponents();

// PanoramicData.Blazor services
builder.Services.AddPanoramicDataBlazor();
// One DumbChatService instance seen through three registrations rather than three instances: the
// conversation history reads the transcripts the chat service holds, so resolving a second
// DumbChatService for it would give the sidebar a store nothing was ever written to.
builder.Services.AddSingleton<DumbChatService>();
builder.Services.AddSingleton<IChatService>(sp => sp.GetRequiredService<DumbChatService>());
builder.Services.AddSingleton<IChatConversationService, DemoChatConversationService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapControllers();
app.MapRazorComponents<App>()
	.AddInteractiveServerRenderMode()
	.AddAdditionalAssemblies(typeof(PanoramicData.Blazor.Demo.Pages.Index).Assembly);

app.Run();
