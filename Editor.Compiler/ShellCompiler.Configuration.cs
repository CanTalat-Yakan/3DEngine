using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Editor.Shell;

public sealed partial class ShellCompiler
{
    /// <summary>Adds a directory to watch for .cs script files.</summary>
    public ShellCompiler WatchDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        _scriptDirectories.Add(Path.GetFullPath(path));
        return this;
    }

    /// <summary>Adds assemblies to the compilation reference set.</summary>
    public ShellCompiler AddReference(Assembly assembly)
    {
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
            _references.Add(MetadataReference.CreateFromFile(location));
        return this;
    }

    /// <summary>Adds a metadata reference directly.</summary>
    public ShellCompiler AddReference(MetadataReference reference)
    {
        _references.Add(reference);
        return this;
    }
}

