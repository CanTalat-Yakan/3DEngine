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
    internal class RuntimeCompiler
    {
        public ComponentCollector ComponentCollector = new();

        private Dictionary<string, Script> scriptsCollection = new();

        public void CompileProjectScripts()
        {
            // Process the list of files found in the directory.
            string scriptsFolderPath = Path.Combine(Files.AssetsPath, "Scripts");
            if (!Directory.Exists(scriptsFolderPath))
                return;

            ScriptOptions scriptOptions = ScriptOptions.Default
                .WithReferences(typeof(Core).Assembly)
                .WithAllowUnsafe(true)
                .WithCheckOverflow(true);

            string[] fileEntries = Directory.GetFiles(scriptsFolderPath);
            foreach (var path in fileEntries)
            {
                string script = File.ReadAllText(path);
                string fileName = Path.GetFileName(path);

                if (scriptsCollection.ContainsKey(fileName))
                {
                    var result = scriptsCollection[fileName].Compile();
                    if (result.Length != 0)
                        Output.Log(result, EMessageType.Error);
                }
                else
                {
                    Script scriptEngine = CSharpScript.Create(script, scriptOptions);
                    var result = scriptEngine.Compile();
                    if (result.Length != 0)
                        Output.Log(string.Join("\r\n", result), EMessageType.Error);

                    scriptsCollection.Add(fileName, scriptEngine);
                }
            }

            CollectComponents();
        }

        public void CollectComponents()
        {
            // Collect all components in the Assembly
            // and ignore all components that have the IHide interface.
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
