namespace Editor.Shell;

// ── Shell Discovery ─────────────────────────────────────────────────────

/// <summary>
/// Marks a class implementing <see cref="IEditorShellBuilder"/> for discovery
/// by the runtime script compiler. The builder's <c>Build()</c> method is called
/// to produce a <see cref="ShellDescriptor"/> tree that drives the editor UI.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EditorShellAttribute : Attribute;