using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Engine;

/// <summary>Roslyn incremental generator scanning [Engine.Behavior] structs and emitting stage systems plus a registration function discoverable at runtime.</summary>
/// <remarks>
/// <para>
/// This generator runs at compile-time and produces two kinds of source outputs:
/// <list type="number">
///   <item><description>Per-behavior <c>{Name}_Generated.g.cs</c> files containing system lambdas for each stage method.</description></item>
///   <item><description>A single <c>BehaviorsRegistration.g.cs</c> file marked with <c>[GeneratedBehaviorRegistration]</c>,
///     discoverable by <c>BehaviorsPlugin</c> at runtime via reflection.</description></item>
/// </list>
/// </para>
/// <para>
/// The private record types <c>BehaviorModel</c>, <c>StageMethod</c>, and <c>Filters</c> form the
/// intermediate representation between Roslyn syntax analysis and source generation.
/// </para>
/// </remarks>
/// <seealso cref="Stage"/>
[Generator(LanguageNames.CSharp)]
public sealed class BehaviorGenerator : IIncrementalGenerator
{
    /// <summary>Configures syntax providers, collects candidate structs, and registers source outputs.</summary>
    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        var candidates = ctx.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is StructDeclarationSyntax sds && sds.AttributeLists.Count > 0,
            static (context, _) =>
            {
                var sds = (StructDeclarationSyntax)context.Node;
                var type = context.SemanticModel.GetDeclaredSymbol(sds);
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
            var (compilation, types) = pair;
            var behaviors = new List<BehaviorModel>();
            foreach (var t in types)
            {
                if (t is null) continue;
                behaviors.Add(BuildModel(t));
            }

            // Emit per-behavior systems
            foreach (var b in behaviors)
                spc.AddSource($"{b.SafeName}.g.cs", GenBehaviorSystems(b));

            // Emit a single registration function discoverable by BehaviorsPlugin via attribute.
            if (behaviors.Count > 0)
                spc.AddSource("BehaviorsRegistration.g.cs", GenRegistration(behaviors));
        });
    }

    /// <summary>Builds a behavior model (namespace, name, stage methods, filters) from a type symbol.</summary>
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
            methods.Add(new StageMethod
            {
                Stage = stage.Value,
                IsStatic = m.IsStatic,
                MethodContainer = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                MethodName = m.Name,
                Filters = GetFilters(m),
                RunIf = GetRunIf(m, type),
                ToggleKey = GetToggleKey(m),
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

    /// <summary>Maps method attributes to a scheduling stage if present.</summary>
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
            if (n == "Engine.OnCleanupAttribute") return Stage.Cleanup;
        }
        return null;
    }

    /// <summary>Extracts With/Without/Changed filters from method attributes.</summary>
    private static Filters GetFilters(IMethodSymbol m)
    {
        var with = new List<string>();
        var without = new List<string>();
        var changed = new List<string>();
        foreach (var a in m.GetAttributes()) 
        {
            var n = a.AttributeClass?.ToDisplayString();
            if (n == "Engine.WithAttribute")
            {
                if (a.ConstructorArguments.Length > 0)
                    foreach (var v in a.ConstructorArguments[0].Values)
                        if (v.Value is ITypeSymbol ts)
                            with.Add(ts.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
            else if (n == "Engine.WithoutAttribute")
            {
                if (a.ConstructorArguments.Length > 0)
                    foreach (var v in a.ConstructorArguments[0].Values)
                        if (v.Value is ITypeSymbol ts)
                            without.Add(ts.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
            else if (n == "Engine.ChangedAttribute")
            {
                if (a.ConstructorArguments.Length > 0)
                    foreach (var v in a.ConstructorArguments[0].Values)
                        if (v.Value is ITypeSymbol ts)
                            changed.Add(ts.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }
        return new Filters(with, without, changed);
    }

    /// <summary>Extracts the [RunIf] condition member (method/property/field) info from a method's attributes.</summary>
    private static (string Name, MemberKind Kind)? GetRunIf(IMethodSymbol method, INamedTypeSymbol behaviorType)
    {
        // Get the attribute name
        string? attrName = null;
        foreach (var a in method.GetAttributes())
        {
            if (a.AttributeClass?.ToDisplayString() == "Engine.RunIfAttribute" &&
                a.ConstructorArguments.Length > 0 &&
                a.ConstructorArguments[0].Value is string name)
            {
                attrName = name;
                break;
            }
        }

        if (attrName is null) return null;

        // Find the member in the behavior type
        foreach (var member in behaviorType.GetMembers())
        {
            if (member.Name != attrName) continue;

            if (member is IMethodSymbol)
                return (attrName, MemberKind.Method);
            else if (member is IPropertySymbol)
                return (attrName, MemberKind.Property);
            else if (member is IFieldSymbol)
                return (attrName, MemberKind.Field);
        }

        return null; // Member not found
    }

    /// <summary>Classifies the kind of member referenced by a [RunIf] attribute.</summary>
    private enum MemberKind { Method, Property, Field }

    /// <summary>Extracts the [ToggleKey] key+modifier pair as raw integers, or null if absent.</summary>
    private static (int Key, int Modifier, bool DefaultEnabled)? GetToggleKey(IMethodSymbol m)
    {
        foreach (var a in m.GetAttributes())
        {
            if (a.AttributeClass?.ToDisplayString() != "Engine.ToggleKeyAttribute") continue;
            var key = a.ConstructorArguments.Length > 0 && a.ConstructorArguments[0].Value is int k ? k : 0;
            var mod = a.ConstructorArguments.Length > 1 && a.ConstructorArguments[1].Value is int mo ? mo : 0;
            var def = true;
            foreach (var na in a.NamedArguments)
                if (na.Key == "DefaultEnabled" && na.Value.Value is bool b)
                    def = b;
            return (key, mod, def);
        }
        return null;
    }

    /// <summary>Generates per-stage system functions and a static Register helper for one behavior.</summary>
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
            var first = g.First();
            var systemId = $"{b.SafeName}_{g.Key}";

            // Fine-grained resource access: instance behaviors write only to their own
            // component store type; static-only behaviors declare a read on EcsWorld.
            // This prevents false write/write conflicts between unrelated behavior types,
            // allowing the parallel scheduler to batch them together.
            bool hasInstanceMethod = g.Any(m => !m.IsStatic);
            string accessMetadata = hasInstanceMethod
                ? $".Write<{b.BehaviorFqn}>()"
                : $".Read<global::Engine.EcsWorld>()";

            string desc;
            if (first.ToggleKey is var (k, mod, defEnabled))
            {
                desc = $"new global::Engine.SystemDescriptor({systemId}, \"{systemId}\")" +
                       $".RunIf(global::Engine.BehaviorConditions.KeyToggle(\"{systemId}\", (global::Engine.Key){k}, (global::Engine.KeyModifier){mod}, {(defEnabled ? "true" : "false")}))" +
                       accessMetadata;
            }
            else if (first.RunIf is var (name, kind))
            {
                string runIfExpr = kind switch
                {
                    MemberKind.Method => $"{b.BehaviorFqn}.{name}",
                    MemberKind.Property => $"_ => {b.BehaviorFqn}.{name}",
                    MemberKind.Field => $"_ => {b.BehaviorFqn}.{name}",
                    _ => throw new InvalidOperationException($"Unknown member kind: {kind}")
                };
                desc = $"new global::Engine.SystemDescriptor({systemId}, \"{systemId}\")" +
                       $".RunIf({runIfExpr})" +
                       accessMetadata;
            }
            else
            {
                desc = $"new global::Engine.SystemDescriptor({systemId}, \"{systemId}\"){accessMetadata}";
            }
            sb.AppendLine($"        app.AddSystem(Engine.Stage.{g.Key}, {desc});");
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
            // non-static: span-based iteration over dense arrays for zero-allocation, cache-linear access
            sb.AppendLine($"        var __store = ecs.GetStorePublic<{b.BehaviorFqn}>();");
            sb.AppendLine($"        var __count = __store.Count;");
            sb.AppendLine($"        for (int __i = 0; __i < __count; __i++)");
            sb.AppendLine("        {");
            sb.AppendLine($"            int entity = __store.EntityByDenseIndex(__i);");
            // Apply filters
            sb.Append(GenFilterChecks(m.Filters));
            sb.AppendLine($"            var behv = __store.ComponentRefByDenseIndex(__i);");
            sb.AppendLine("            ctx.EntityId = entity;");
            sb.AppendLine($"            behv.{m.MethodName}(ctx);");
            // Write back the mutated struct directly into the dense array (no dict lookup, no change-bit overhead)
            sb.AppendLine($"            __store.ComponentRefByDenseIndex(__i) = behv;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Generates a foreach header for multi-component queries (retained for future use with [With] filter joins).</summary>
    private static string GenForeachHeader(string behaviorFqn, IReadOnlyList<string> with)
    {
        // Prefer joining via typed queries when up to 3 with-filters are present, else fallback to single-type scan.
        return with.Count switch
        {
            0 => $"        foreach (var (entity, behv) in ecs.Query<{behaviorFqn}>())\n        {{",
            1 => $"        foreach (var (entity, behv, __w1) in ecs.Query<{behaviorFqn}, {with[0]}>())\n        {{",
            2 => $"        foreach (var (entity, behv, __w1, __w2) in ecs.Query<{behaviorFqn}, {with[0]}, {with[1]}>())\n        {{",
            3 => $"        foreach (var (entity, behv, __w1, __w2, __w3) in ecs.Query<{behaviorFqn}, {with[0]}, {with[1]}, {with[2]}>())\n        {{",
            _ => $"        foreach (var (entity, behv) in ecs.Query<{behaviorFqn}>())\n        {{",
        };
    }

    private static string GenFilterChecks(Filters f)
    {
        var sb = new StringBuilder();
        // Always enforce With as guards to cover the fallback iteration paths (and as a cheap safety-net otherwise).
        foreach (var w in f.With)
            sb.AppendLine($"            if (!ecs.Has<{w}>(entity)) continue;");
        foreach (var wout in f.Without)
            sb.AppendLine($"            if (ecs.Has<{wout}>(entity)) continue;");
        foreach (var ch in f.Changed)
            sb.AppendLine($"            if (!ecs.Changed<{ch}>(entity)) continue;");
        return sb.ToString();
    }

    /// <summary>Emits a registration method marked with [GeneratedBehaviorRegistration] that registers all discovered behaviors.</summary>
    private static string GenRegistration(IEnumerable<BehaviorModel> behaviors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("namespace Engine;");
        sb.AppendLine();
        sb.AppendLine("public static class BehaviorRegistration");
        sb.AppendLine("{");
        sb.AppendLine("    [global::Engine.GeneratedBehaviorRegistration]");
        sb.AppendLine("    public static void Register(global::Engine.App app)");
        sb.AppendLine("    {");
        foreach (var b in behaviors)
            sb.AppendLine($"        global::{b.Namespace}.{b.SafeName}.Register(app);");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Scheduling stage for generated system registration.</summary>
    private enum Stage { Startup, First, PreUpdate, Update, PostUpdate, Render, Last, Cleanup }

    /// <summary>Component filter configuration extracted from [With], [Without], [Changed] attributes.</summary>
    /// <param name="With">Component types that must be present on the entity.</param>
    /// <param name="Without">Component types that must be absent from the entity.</param>
    /// <param name="Changed">Component types that must have been modified since the last frame.</param>
    private sealed record Filters(IReadOnlyList<string> With, IReadOnlyList<string> Without, IReadOnlyList<string> Changed);

    /// <summary>Represents a single stage-annotated method within a behavior struct.</summary>
    private sealed record StageMethod
    {
        /// <summary>The scheduling stage this method runs in.</summary>
        public Stage Stage { get; init; }
        /// <summary>Whether the method is static (global) or instance (per-entity).</summary>
        public bool IsStatic { get; init; }
        /// <summary>Fully-qualified name of the type containing the method.</summary>
        public string MethodContainer { get; init; } = string.Empty;
        /// <summary>Simple name of the method.</summary>
        public string MethodName { get; init; } = string.Empty;
        /// <summary>Component filters applied to this method.</summary>
        public Filters Filters { get; init; } = new Filters(Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>());
        /// <summary>Optional [RunIf] condition reference (member name and kind).</summary>
        public (string Name, MemberKind Kind)? RunIf { get; init; }
        /// <summary>Optional [ToggleKey] key binding (key code, modifier, default enabled state).</summary>
        public (int Key, int Modifier, bool DefaultEnabled)? ToggleKey { get; init; }
    }

    /// <summary>Aggregated model for a single [Behavior]-annotated struct and its stage methods.</summary>
    private sealed record BehaviorModel
    {
        /// <summary>Namespace of the behavior type.</summary>
        public string Namespace { get; init; } = "Engine";
        /// <summary>Simple type name of the behavior struct.</summary>
        public string Name { get; init; } = string.Empty;
        /// <summary>Generated helper class name (unique per behavior).</summary>
        public string SafeName { get; init; } = string.Empty;
        /// <summary>Fully-qualified name of the behavior struct.</summary>
        public string BehaviorFqn { get; init; } = string.Empty;
        /// <summary>All stage-annotated methods discovered on this behavior.</summary>
        public List<StageMethod> StageMethods { get; init; } = new();
    }
}
