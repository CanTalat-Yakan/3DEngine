using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;

namespace Engine.RuntimeSystem;

internal sealed class ScriptEntry
{
    public FileInfo FileInfo;
    public Script<object> Script;
    public Assembly Assembly;
}

public sealed class ComponentCollector
{
    public List<Type> Components = new();

    public Type GetComponent(string name) =>
        Components.Find(Type => Type.Name.ToString() == name);
}

public sealed class ScriptCompiler
{
    public static ComponentCollector ComponentCollector = new();

    private Dictionary<string, ScriptEntry> _scriptsCollection = new();

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
            ScriptEntry scriptEntry = GetScriptEntry(path);

            if (scriptEntry is not null)
                CompileScript(scriptEntry);
        }

        RemoveObsoleteScripts();
        CollectComponents();
    }

    private static ScriptOptions CreateScriptOptions() =>
        ScriptOptions.Default
            .WithImports("System")
            .WithReferences(typeof(Core).Assembly)
            .WithAllowUnsafe(true)
            .WithCheckOverflow(true);

    private ScriptEntry GetScriptEntry(string path)
    {
        FileInfo fileInfo = new(path);

        if (_scriptsCollection.TryGetValue(fileInfo.FullName, out ScriptEntry scriptEntry))
        {
            if (fileInfo.LastWriteTime > scriptEntry.FileInfo.LastWriteTime)
            {
                scriptEntry.FileInfo = fileInfo;
                string updatedCode = File.ReadAllText(path);
                scriptEntry.Script = CSharpScript.Create(updatedCode, CreateScriptOptions());

                Output.Log("Updated File");
            }
            else
                scriptEntry = null;
        }
        else
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(fs))
            {
                scriptEntry = new ScriptEntry() { FileInfo = fileInfo };
                string code = reader.ReadToEnd();
                scriptEntry.Script = CreateScript(code);
                _scriptsCollection.Add(fileInfo.FullName, scriptEntry);

                Output.Log("Read new File");
            }
        }

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
        {
            Output.Log(
                string.Join("\r\n", error),
                error.WarningLevel == 0
                    ? MessageType.Error
                    : MessageType.Warning,
                error.Location.GetLineSpan().StartLinePosition.Line,
                null,
                scriptEntry?.FileInfo.Name);
        }
    }

    private void RemoveObsoleteScripts()
    {
        foreach (var fullName in _scriptsCollection.Keys.ToArray())
            if (!_scriptsCollection[fullName].FileInfo.Exists)
            {
                var assembly = _scriptsCollection[fullName].Assembly;
                _ignoreAssemblies.Add(assembly);

                DestroyComponentTypeReferences(assembly);

                _scriptsCollection.Remove(fullName);

                Output.Log("Removed File");
            }
    }

    private void CollectComponents()
    {
        var componentCollection = _allAssemblies
            .Except(_ignoreAssemblies)
            .SelectMany(a => a.GetTypes())
            .Where(t =>
                typeof(Component).IsAssignableFrom(t) && !t.Equals(typeof(Component))
                && !(typeof(IHide).IsAssignableFrom(t) && !t.IsInterface))
            .ToArray();

        ComponentCollector.Components.Clear();
        ComponentCollector.Components.AddRange(componentCollection);
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
        foreach (var type in assembly.GetTypes())
            if (type.IsSubclassOf(typeof(Component)))
                foreach (var ignoreAssembly in _ignoreAssemblies)
                    foreach (var ignoreType in ignoreAssembly.GetTypes())
                        if (type.FullName == ignoreType.FullName)
                            ScriptSystem.Replace(ignoreType, type);
    }
}