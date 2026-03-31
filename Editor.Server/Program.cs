using BlazorBlueprint.Components;
using Editor.Server;

// When run standalone, start the server and block until shutdown.
var server = await EditorServerHost.StartAsync(args: args);
await server.WaitForShutdownAsync();

namespace Editor.Server
{
    /// <summary>
    /// Configures and starts the Editor.Server Blazor application.
    /// Can be hosted in-process from the Editor or run standalone.
    /// </summary>
    public static class EditorServerHost
    {
        /// <summary>
        /// Builds and starts the Blazor Server on the given URL without blocking.
        /// Returns the running <see cref="WebApplication"/> so the caller can stop it later.
        /// </summary>
        public static async Task<WebApplication> StartAsync(string url = "http://localhost:5000", string[]? args = null)
        {
            // ApplicationName must point to this assembly so the static web assets
            // pipeline (wwwroot, _content/, CSS) resolves correctly even when
            // hosted in-process from the Editor executable.
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args ?? [],
                ApplicationName = typeof(EditorServerHost).Assembly.GetName().Name!,
            });
            builder.WebHost.UseUrls(url);

            // Always enable static web assets resolution so CSS/JS from
            // Editor.Server/wwwroot and NuGet packages (_content/) are found,
            // even when the environment isn't Development (e.g., hosted from Editor).
            builder.WebHost.UseStaticWebAssets();

            // Add services to the container
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            // Add BlazorBlueprint services
            builder.Services.AddBlazorBlueprintComponents();

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            await app.StartAsync();
            return app;
        }
    }
}
