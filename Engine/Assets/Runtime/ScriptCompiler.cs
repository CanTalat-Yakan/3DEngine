using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;

namespace Engine.Runtime;

public sealed class ScriptEntry
{
    public FileInfo FileInfo;
    public Script<object> Script;
    public Assembly Assembly;
}

public sealed class ScriptCompiler
{
    private List<Assembly> _allAssemblies = new();
    private List<Assembly> _ignoreAssemblies = new();

    public void CompileProjectScripts(string assetsPath = null)
    {
        if (assetsPath is null)
            return;

        string scriptsFolderPath = Path.Combine(assetsPath, "Scripts");
        if (!Directory.Exists(scriptsFolderPath))
            return;

        foreach (var path in Directory.GetFiles(scriptsFolderPath, "*", SearchOption.AllDirectories))
        {
            var scriptEntry = GetScriptEntry(path);
            if (scriptEntry is not null)
                CompileScript(scriptEntry);
        }

        RemoveObsoleteScripts();
        CollectComponents();
    }

    private static ScriptOptions CreateScriptOptions() =>
        ScriptOptions.Default
            .WithImports("System", "System.Collections.Generic", "System.Linq")
            .WithReferences(typeof(Kernel).Assembly)
            .WithAllowUnsafe(true)
            .WithCheckOverflow(true);

    private ScriptEntry GetScriptEntry(string path)
    {
        FileInfo fileInfo = new(path);

        if (!Assets.Scripts.TryGetValue(fileInfo.FullName, out var scriptEntry))
        {
            if (path.IsFileLocked().Value)
                throw new Exception("File is locked and cannot be read");

            using FileStream fileStream = new(path, FileMode.Open, FileAccess.Read);
            using StreamReader streamReader = new(fileStream);

            scriptEntry = new ScriptEntry() { FileInfo = fileInfo };
            string code = streamReader.ReadToEnd();
            scriptEntry.Script = CreateScript(code);
            Assets.Scripts.Add(fileInfo.FullName, scriptEntry);

            Output.Log("Read new Script");
        }
        else if (fileInfo.LastWriteTime > scriptEntry.FileInfo.LastWriteTime)
        {
            scriptEntry.FileInfo = fileInfo;
            string updatedCode = File.ReadAllText(path);
            scriptEntry.Script = CSharpScript.Create(updatedCode, CreateScriptOptions());

            Output.Log("Updated Script");
        }
        else
            scriptEntry = null;

        return scriptEntry;
    }

    internal static Script<object> CreateScript(string code) =>
        CSharpScript.Create(code, CreateScriptOptions());

    internal void CompileScript(ScriptEntry scriptEntry)
    {
        try
        {
            Compilation compilation = scriptEntry.Script.GetCompilation();

            compilation = compilation.WithOptions(compilation.Options
               .WithOptimizationLevel(OptimizationLevel.Debug)
               .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            using (var assemblyStream = new MemoryStream())
            using (var symbolStream = new MemoryStream())
            {
                var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
                var result = compilation.Emit(assemblyStream, symbolStream, options: emitOptions);

                if (!result.Success)
                {
                    LogCompilationErrors(result, scriptEntry);

                    return;
                }

                if (scriptEntry.Assembly != null)
                    _ignoreAssemblies.Add(scriptEntry.Assembly);

                scriptEntry.Assembly = Assembly.Load(assemblyStream.ToArray(), symbolStream.ToArray());
                ReplaceComponentTypeReferences(scriptEntry.Assembly);

                _allAssemblies.Add(scriptEntry.Assembly);

                Output.Log("Loaded Assembly");
            }
        }
        catch { }
    }

    private void LogCompilationErrors(EmitResult result, ScriptEntry scriptEntry)
    {
        foreach (var error in result.Diagnostics)
            Output.Log(
                string.Join("\r\n", error),
                error.WarningLevel == 0
                    ? MessageType.Error
                    : MessageType.Warning,
                error.Location.GetLineSpan().StartLinePosition.Line,
                null,
                scriptEntry?.FileInfo.Name);
    }

    private void RemoveObsoleteScripts()
    {
        foreach (var fullName in Assets.Scripts.Keys.ToArray())
            if (!Assets.Scripts[fullName].FileInfo.Exists)
            {
                var assembly = Assets.Scripts[fullName].Assembly;
                _ignoreAssemblies.Add(assembly);

                DestroyComponentTypeReferences(assembly);

                Assets.Scripts.Remove(fullName);

                Output.Log("Removed File");
            }
    }

    private void CollectComponents()
    {
        var componentCollection = _allAssemblies
            .Except(_ignoreAssemblies)
            .SelectMany(Assembly => Assembly.GetTypes())
            .Where(Type =>
                (typeof(Component).IsAssignableFrom(Type) || typeof(EditorComponent).IsAssignableFrom(Type))
                && !Type.Equals(typeof(Component))
                && !Type.Equals(typeof(EditorComponent))
                && !(typeof(IHide).IsAssignableFrom(Type)
                && !Type.IsInterface))
            .ToArray();

        Assets.Components.Clear(); 
        Assets.Components = componentCollection.ToDictionary(
            component => component.Name.ToString(), 
            component => component);
    }

    private void DestroyComponentTypeReferences(Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
            if (type.IsSubclassOf(typeof(EditorComponent)))
                EditorScriptSystem.Destroy(type);
            else if (type.IsSubclassOf(typeof(Component)))
                ScriptSystem.Destroy(type);
    }

    private void ReplaceComponentTypeReferences(Assembly assembly)
    {
        foreach (var ignoreAssembly in _ignoreAssemblies)
            foreach (var ignoreType in ignoreAssembly.GetTypes())
            {
                foreach (var type in assembly.GetTypes())
                    if (type.FullName.Equals(ignoreType.FullName))
                    {
                        if (type.IsSubclassOf(typeof(Component)))
                            ScriptSystem.Replace(ignoreType, type);
                        else if (type.IsSubclassOf(typeof(EditorComponent)))
                            EditorScriptSystem.Replace(ignoreType, type);
                    }
            }
    }
}