using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Engine.Utilities
{
    internal class ScriptEntry
    {
        public FileInfo FileInfo;
        public Script<object> Script;
        public Assembly Assembly;
    }

    internal class RuntimeCompiler
    {
        public ComponentCollector ComponentCollector = new();

        private Dictionary<string, ScriptEntry> _scriptsCollection = new();

        private List<Assembly> _ignoreAssemblies = new();

        public void CompileProjectScripts()
        {
            // Process the list of files found in the directory.
            string scriptsFolderPath = Path.Combine(Files.AssetsPath, "Scripts");
            if (!Directory.Exists(scriptsFolderPath))
                return;

            // create a new scriptEngine and options to be used in the loop.
            ScriptOptions scriptOptions = ScriptOptions.Default
                .WithReferences(typeof(Core).Assembly)
                .WithAllowUnsafe(true)
                .WithCheckOverflow(true);

            List<string> validateScripts = new();

            string[] fileEntries = Directory.GetFiles(scriptsFolderPath);
            foreach (var path in fileEntries)
            {
                ScriptEntry scriptEntry = null;
                FileInfo fileInfo = new(path);

                // Check if dictionary contains the file info.
                if (_scriptsCollection.ContainsKey(fileInfo.FullName))
                {
                    scriptEntry = _scriptsCollection[fileInfo.FullName];

                    // Check if the file has been modified.
                    if (fileInfo.LastWriteTime > scriptEntry.FileInfo.LastWriteTime)
                    {
                        // Update the file info in the scene entry with the new lastWriteTime.
                        scriptEntry.FileInfo = fileInfo;

                        // Create a new script from the file.
                        string updatedCode = File.ReadAllText(path);
                        scriptEntry.Script = CSharpScript.Create(updatedCode, scriptOptions);

                        Output.Log("Updated file");
                    }
                    else
                        scriptEntry = null;
                }
                else
                {
                    scriptEntry = new() { FileInfo = fileInfo };

                    // Create a new script from the file.
                    string code = File.ReadAllText(path);
                    scriptEntry.Script = CSharpScript.Create(code, scriptOptions);

                    // Add it into the collection.
                    _scriptsCollection.Add(fileInfo.FullName, scriptEntry);

                    Output.Log("Created new file");
                }

                validateScripts.Add(fileInfo.FullName);

                // If the scriptEntry is set, compile the script.
                // It is set when the script is newly created or updated.
                if (scriptEntry is not null)
                {
                    // Compilation gives access to the full set of Roslyn APIs.
                    var compilation = scriptEntry.Script.GetCompilation();
                    compilation = compilation.WithOptions(compilation.Options
                       .WithOptimizationLevel(OptimizationLevel.Debug)
                       .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

                    // Compile script with reference to the assembly.
                    // It is worth noting that we used OptimizationLevel.Debug which disables all optimizations what improves debugging experience.
                    // The result of compilations are two streams: assemblyStream contains actual compiled code and symbolStream contains symbols.
                    using (var assemblyStream = new MemoryStream())
                    using (var symbolStream = new MemoryStream())
                    {
                        var emitOptions = new EmitOptions(false, DebugInformationFormat.PortablePdb);
                        var result = compilation.Emit(assemblyStream, symbolStream, options: emitOptions);

                        if (!result.Success)
                            foreach (var error in result.Diagnostics)
                                Output.Log(
                                    string.Join("\r\n", error),
                                    error.WarningLevel == 0
                                        ? MessageType.Error
                                        : MessageType.Warning);

                        // Add assembly to list to ignore in the "CollectComponents" method,
                        // when the an assembly reference is inside of the script entry.
                        if (scriptEntry.Assembly is not null)
                            _ignoreAssemblies.Add(scriptEntry.Assembly);

                        // Load the assenbly with the compiled script.
                        scriptEntry.Assembly = Assembly.Load(assemblyStream.ToArray(), symbolStream.ToArray());

                        // Replace all matching components with the new one.
                        ReplaceComponentTypeReferences(scriptEntry.Assembly);

                        Output.Log("Loaded assembly");
                    }
                }
            }

            // Remove all compiled project script that are not validated.
            foreach (var fullName in _scriptsCollection.Keys.ToArray())
                if (!validateScripts.Contains(fullName))
                {
                    var assembly = _scriptsCollection[fullName].Assembly;

                    // Add assembly to list to ignore in the "CollectComponents" method,
                    // with the assembly reference of the script entry that got deleted.
                    _ignoreAssemblies.Add(assembly);

                    DestroyComponentTypeReferences(assembly);

                    // Remove script from collection.
                    _scriptsCollection.Remove(fullName);
                    Output.Log("Removed file");
                }

            // Gather components for the editor's "AddComponent" function.
            CollectComponents();
        }

        public void CollectComponents()
        {
            // Collect all components in the Assembly
            // and ignore all components that have the "IHide" interface.
            var componentCollection = AppDomain.CurrentDomain.GetAssemblies()
                .Except(_ignoreAssemblies)
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    typeof(Component).IsAssignableFrom(t) && !t.Equals(typeof(Component))
                    && !(typeof(IHide).IsAssignableFrom(t) && !t.IsInterface))
                .ToArray();

            // Add components to the collector.
            ComponentCollector.Components.Clear();
            ComponentCollector.Components.AddRange(componentCollection);
        }

        private void DestroyComponentTypeReferences(Assembly assembly)
        {
            // Remove the specified components in the script or editorscript system,
            // using the types obtained from the provided assembly.
            foreach (var type in assembly.GetTypes())
                if (type.IsSubclassOf(typeof(EditorComponent)))
                    EditorScriptSystem.Destroy(type);
                else if (type.IsSubclassOf(typeof(Component)))
                    ScriptSystem.Destroy(type);
        }

        private void ReplaceComponentTypeReferences(Assembly assembly)
        {
            // Remove the specified components in the script- or editorscript system,
            // using the types obtained from the provided assembly.
            foreach (var type in assembly.GetTypes())
                if (type.IsSubclassOf(typeof(Component)))
                    foreach (var ignoreAssembly in _ignoreAssemblies)
                        foreach (var ignoreType in ignoreAssembly.GetTypes())
                            if (type.IsSubclassOf(typeof(Component)))
                                if (type.FullName == ignoreType.FullName)
                                    ScriptSystem.Replace(ignoreType, type);
        }
    }
}
