using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine.SourceGen;

/// <summary> Roslyn incremental generator scanning [Engine.Behavior] structs and emitting stage systems plus a registration plugin. </summary>
[Generator(LanguageNames.CSharp)]
public sealed class EcsBehaviorGenerator : IIncrementalGenerator
{
    /// <summary> Configures syntax providers, collects candidate structs, and registers source outputs. </summary>
    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        var candidates = ctx.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is StructDeclarationSyntax sds && sds.AttributeLists.Count > 0,
            static (context, _) =>
            {
                var sds = (StructDeclarationSyntax)context.Node;
                var type = context.SemanticModel.GetDeclaredSymbol(sds) as INamedTypeSymbol;
                if (type is null) return null;
                foreach (var a in type.GetAttributes())
                    if (a.AttributeClass?.ToDisplayString() == "Engine.BehaviorAttribute")
                        return type;
                return null;
            })
            .Where(s => s is not null)
            .Collect();

        var compilationAndTypes = ctx.CompilationProvider.Combine(candidates);
        ctx.RegisterSourceOutput(compilationAndTypes, (spc, pair) =>
        {
            var (_, types) = pair; // discard compilation
            var behaviors = new List<BehaviorModel>();
            foreach (var t in types)
            {
                if (t is null) continue;
                var model = BuildModel(t);
                behaviors.Add(model);
            }

            // Emit per-behavior systems
            foreach (var b in behaviors)
                spc.AddSource($"{b.SafeName}.g.cs", GenBehaviorSystems(b));

            // Emit registration plugin
            spc.AddSource("BehaviorsPlugin.g.cs", GenPlugin(behaviors));
        });
    }

    /// <summary> Builds a behavior model (namespace, name, stage methods, filters) from a type symbol. </summary>
    private static BehaviorModel BuildModel(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace.IsGlobalNamespace ? "Engine" : type.ContainingNamespace.ToDisplayString();
        var name = type.Name;
        var safe = name + "_Generated"; // ensure uniqueness per behavior type
        var methods = new List<StageMethod>();

        foreach (var m in type.GetMembers().OfType<IMethodSymbol>())
        {
            Stage? stage = GetStage(m);
            if (stage is null) continue;
            // Support instance and static methods
            var filters = GetFilters(m);
            methods.Add(new StageMethod
            {
                Stage = stage.Value,
                IsStatic = m.IsStatic,
                MethodContainer = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                MethodName = m.Name,
                Filters = filters
            });
        }

        return new BehaviorModel
        {
            Namespace = ns,
            Name = name,
            SafeName = safe,
            BehaviorFqn = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            StageMethods = methods
        };
    }

    /// <summary> Maps method attributes to a scheduling stage if present. </summary>
    private static Stage? GetStage(IMethodSymbol m)
    {
        foreach (var a in m.GetAttributes())
        {
            var n = a.AttributeClass?.ToDisplayString();
            if (n == "Engine.OnStartupAttribute") return Stage.Startup;
            if (n == "Engine.OnFirstAttribute") return Stage.First;
            if (n == "Engine.OnPreUpdateAttribute") return Stage.PreUpdate;
            if (n == "Engine.OnUpdateAttribute") return Stage.Update;
            if (n == "Engine.OnPostUpdateAttribute") return Stage.PostUpdate;
            if (n == "Engine.OnRenderAttribute") return Stage.Render;
            if (n == "Engine.OnLastAttribute") return Stage.Last;
        }
        return null;
    }

    /// <summary> Extracts With/Without/Changed filters from method attributes. </summary>
    private static Filters GetFilters(IMethodSymbol m)
    {
        var with = new List<string>();
        var without = new List<string>();
        var changed = new List<string>();
        foreach (var a in m.GetAttributes())
        {
            var n = a.AttributeClass?.ToDisplayString();
            if (n == "Engine.WithAttribute")
                with.AddRange(a.ConstructorArguments[0].Values.Select(v => v.Value!.ToString()!));
            else if (n == "Engine.WithoutAttribute")
                without.AddRange(a.ConstructorArguments[0].Values.Select(v => v.Value!.ToString()!));
            else if (n == "Engine.ChangedAttribute")
                changed.AddRange(a.ConstructorArguments[0].Values.Select(v => v.Value!.ToString()!));
        }
        return new Filters(with, without, changed);
    }

    /// <summary> Generates per-stage system functions and a static Register helper for one behavior. </summary>
    private static string GenBehaviorSystems(BehaviorModel b)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine($"namespace {b.Namespace};");
        sb.AppendLine();
        sb.AppendLine($"internal static class {b.SafeName}");
        sb.AppendLine("{");
        sb.AppendLine("    public static void Register(Engine.App app)");
        sb.AppendLine("    {");
        foreach (var g in b.StageMethods.GroupBy(m => m.Stage))
        {
            sb.AppendLine($"        app.AddSystem(Engine.Stage.{g.Key}, {b.SafeName}_{g.Key});");
        }
        sb.AppendLine("    }");
        foreach (var m in b.StageMethods)
        {
            sb.AppendLine();
            sb.AppendLine($"    private static void {b.SafeName}_{m.Stage}(Engine.World world)");
            sb.AppendLine("    {");
            sb.AppendLine("        var ecs = world.Resource<Engine.EcsWorld>();");
            sb.AppendLine("        var ctx = new Engine.BehaviorContext(world);");
            if (m.IsStatic)
            {
                sb.AppendLine($"        {m.MethodContainer}.{m.MethodName}(ctx);");
                sb.AppendLine("        return;");
                sb.AppendLine("    }");
                continue;
            }
            // non-static: iterate entities
            var loopHeader = GenForeachHeader(b.BehaviorFqn, m.Filters.With);
            sb.AppendLine(loopHeader);
            // Filters: Without / Changed
            sb.Append(GenFilterChecks(m.Filters));
            sb.AppendLine("            ctx.EntityId = entity;");
            sb.AppendLine($"            behv.{m.MethodName}(ctx);");
            sb.AppendLine($"            ecs.Update<{b.BehaviorFqn}>(entity, behv);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenForeachHeader(string behaviorFqn, IReadOnlyList<string> with)
    {
        return with.Count switch
        {
            0 => $"        foreach (var (entity, behv) in ecs.Query<{behaviorFqn}>())\n        {{",
            1 => $"        foreach (var (entity, behv, __w1) in ecs.Query<{behaviorFqn}, {with[0]}>())\n        {{",
            2 => $"        foreach (var (entity, behv, __w1, __w2) in ecs.Query<{behaviorFqn}, {with[0]}, {with[1]}>())\n        {{",
            _ => $"        foreach (var (entity, behv) in ecs.Query<{behaviorFqn}>())\n        {{",
        };
    }

    private static string GenFilterChecks(Filters f)
    {
        var sb = new StringBuilder();
        foreach (var wout in f.Without)
            sb.AppendLine($"            if (ecs.Has<{wout}>(entity)) continue;");
        foreach (var ch in f.Changed)
            sb.AppendLine($"            if (!ecs.Changed<{ch}>(entity)) continue;");
        return sb.ToString();
    }

    /// <summary> Emits the partial BehaviorsPlugin implementation registering all discovered behaviors. </summary>
    private static string GenPlugin(IEnumerable<BehaviorModel> behaviors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("namespace Engine;");
        // Emit implementation for partial method inside partial class
        sb.AppendLine("public sealed partial class BehaviorsPlugin");
        sb.AppendLine("{");
        sb.AppendLine("    static partial void BuildGenerated(Engine.App app)");
        sb.AppendLine("    {");
        foreach (var b in behaviors)
            sb.AppendLine($"        global::{b.Namespace}.{b.SafeName}.Register(app);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private enum Stage { Startup, First, PreUpdate, Update, PostUpdate, Render, Last }
    private sealed record Filters(IReadOnlyList<string> With, IReadOnlyList<string> Without, IReadOnlyList<string> Changed);
    private sealed record StageMethod
    {
        public Stage Stage { get; init; }
        public bool IsStatic { get; init; }
        public string MethodContainer { get; init; } = string.Empty;
        public string MethodName { get; init; } = string.Empty;
        public Filters Filters { get; init; } = new Filters(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
    }
    private sealed record BehaviorModel
    {
        public string Namespace { get; init; } = "Engine";
        public string Name { get; init; } = string.Empty;
        public string SafeName { get; init; } = string.Empty;
        public string BehaviorFqn { get; init; } = string.Empty;
        public List<StageMethod> StageMethods { get; init; } = new();
    }
}
