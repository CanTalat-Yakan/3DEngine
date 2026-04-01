using Editor.Shell;
using Microsoft.AspNetCore.Components;

namespace Editor.Server.Shared;

public partial class ShellLayout : LayoutComponentBase, IDisposable
{
    [Inject] private ShellRegistry Registry { get; set; } = null!;

    private ShellDescriptor Descriptor => Registry.Current;

    protected override void OnInitialized()
    {
        Registry.Changed += OnShellChanged;
    }

    private void OnShellChanged()
    {
        InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        Registry.Changed -= OnShellChanged;
    }
}
