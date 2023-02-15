using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Editor.Controller;
using Engine.ECS;
using Engine.Editor;

namespace Engine.Utilities
{
    internal class ScriptEntry
    {
        public FileInfo FileInfo;
        public Script<object> Script;
    }

    internal class RuntimeCompiler
    {
        public ComponentCollector ComponentCollector = new();

        private Dictionary<string, ScriptEntry> scriptsCollection = new();

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
                if (scriptsCollection.ContainsKey(fileInfo.FullName))
                {
                    scriptEntry = scriptsCollection[fileInfo.FullName];

                    // Check if the file has been modified.
                    if (fileInfo.LastWriteTime > scriptEntry.FileInfo.LastWriteTime)
                    {
                        // Update the file info in the scene entry with the new lastWriteTime.
                        scriptEntry.FileInfo = fileInfo;

                        // Create a new script from the file.
                        string updatedCode = File.ReadAllText(path);
                        scriptEntry.Script = CSharpScript.Create(updatedCode, scriptOptions);

                        Output.Log("Updated the file");
                    }
                }
                else
                {
                    scriptEntry = new() { FileInfo = fileInfo };

                    // Create a new script from the file.
                    string code = File.ReadAllText(path);
                    scriptEntry.Script = CSharpScript.Create(code, scriptOptions);

                    // Add it into the collection.
                    scriptsCollection.Add(fileInfo.FullName, scriptEntry);

                    Output.Log("Created new file");
                }

                validateScripts.Add(fileInfo.FullName);

                // If the scriptEntry is set, compile the script.
                if (scriptEntry is not null)
                {
                    var results = scriptEntry.Script.Compile();
                    if (results.Length != 0)
                        foreach (var result in results)
                            Output.Log(
                                string.Join("\r\n", result), 
                                result.WarningLevel == 0 
                                    ? EMessageType.Error
                                    : EMessageType.Warning);
                }
            }

            // Remove all compiled project script.
            foreach (var fullName in scriptsCollection.Keys.ToArray())
                if (!validateScripts.Contains(fullName))
                {
                    // Remove script from collection.
                    scriptsCollection.Remove(fullName);
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
                .SelectMany(s => s.GetTypes())
                .Where(p =>
                    (typeof(Component).IsAssignableFrom(p) && !p.Equals(typeof(Component)))
                    && !(typeof(IHide).IsAssignableFrom(p) && !p.IsInterface))
                .ToArray();

            // Add components to the collector.
            ComponentCollector.Components.Clear();
            ComponentCollector.Components.AddRange(componentCollection.ToArray());
        }
    }
}
