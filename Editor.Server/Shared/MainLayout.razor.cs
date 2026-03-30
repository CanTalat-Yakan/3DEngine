using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Editor.Server.Shared;

public partial class MainLayout : LayoutComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private bool isDarkMode;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            isDarkMode = await JS.InvokeAsync<bool>("isDarkModeEnabled");
            StateHasChanged();
        }
    }

    private async Task ToggleDarkMode()
    {
        await JS.InvokeVoidAsync("toggleDarkMode");
        isDarkMode = !isDarkMode;
    }
}