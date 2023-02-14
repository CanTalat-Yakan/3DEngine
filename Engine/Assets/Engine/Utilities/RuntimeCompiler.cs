using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
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
        public Script Script;
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
            Script scriptEngine;
            ScriptOptions scriptOptions = ScriptOptions.Default
                //.WithImports("")
                .WithReferences(typeof(Core).Assembly)
                .WithAllowUnsafe(true)
                .WithCheckOverflow(true);

            List<string> validateScripts = new();

            string[] fileEntries = Directory.GetFiles(scriptsFolderPath);
            foreach (var path in fileEntries)
            {
                scriptEngine = null;

                FileInfo fileInfo = new(path);
                string code = File.ReadAllText(path);

                // Check if dictionary contains the file info.
                if (scriptsCollection.ContainsKey(fileInfo.FullName))
                {
                    var scriptEntry = scriptsCollection[fileInfo.FullName];

                    // Check if the file has been modified.
                    if (fileInfo.LastWriteTime > scriptEntry.FileInfo.LastWriteTime)
                    {
                        // Update the file info in the scene entry with the new lastWriteTime.
                        scriptEntry.FileInfo = fileInfo;
                        // Get the script from the file info.
                        scriptEngine = scriptEntry.Script;

                        // Continue script with new code from the file.
                        scriptEngine.ContinueWith(code);
                        Output.Log("Updated the file");
                    }
                }
                else
                {
                    // Create a new script from the file.
                    scriptEngine = CSharpScript.Create(code, scriptOptions);

                    // Add it into the collection.
                    scriptsCollection.Add(fileInfo.FullName, new() { FileInfo = fileInfo, Script = scriptEngine });
                    Output.Log("Created new file");
                }

                validateScripts.Add(fileInfo.FullName);

                // Compile the script when it was created or updated.
                if (scriptEngine is not null)
                {
                    var result = scriptEngine.Compile();
                    if (result.Length != 0)
                        Output.Log(string.Join("\r\n", result), EMessageType.Error);
                }
            }

            // Remove all compiled project script.
            foreach (var fullName in scriptsCollection.Keys.ToArray())
                if (!validateScripts.Contains(fullName))
                {
                    scriptsCollection.Remove(fullName);

                    // Remove script from assembly.
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
