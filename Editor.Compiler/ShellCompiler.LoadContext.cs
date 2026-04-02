using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;

namespace Editor.Shell;

public sealed partial class ShellCompiler
{
    private void UnloadCurrent()
    {
        if (_currentContext == null) return;
        _currentContext.Unload();
        _currentContext = null;
    }

    private void AddDefaultReferences()
    {
        // Core runtime assemblies
        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (trustedPlatformAssemblies != null)
        {
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                var fileName = Path.GetFileName(path);
                // Include essential runtime assemblies
                if (fileName.StartsWith("System.") || fileName.StartsWith("Microsoft.") ||
                    fileName == "mscorlib.dll" || fileName == "netstandard.dll")
                {
                    try { _references.Add(MetadataReference.CreateFromFile(path)); }
                    catch { /* skip inaccessible assemblies */ }
                }
            }
        }

        // Add Editor.Shell itself (so scripts can reference the builder API)
        AddReference(typeof(ShellRegistry).Assembly);
    }

    private sealed class ScriptLoadContext(string name) : AssemblyLoadContext(name, isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName) => null;
    }
}


