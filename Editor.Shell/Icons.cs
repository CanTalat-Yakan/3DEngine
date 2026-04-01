namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Icon Reference — type-safe icon identifiers supporting multiple icon sets.
//
//  Usage (preferred — just pass the enum value directly):
//    icon: Lucide.BookOpen
//    icon: Feather.Camera
//    icon: Heroicon.AcademicCap
//
//  Usage (string fallback for custom / unlisted icons):
//    icon: IconRef.FromName(IconSet.Lucide, "book-open")
//
//  Implicit conversions from Lucide, Feather, and Heroicon enums to IconRef
//  are provided, so any parameter of type IconRef accepts enum values directly.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Which icon library to use when rendering.</summary>
public enum IconSet
{
    Lucide,
    Feather,
    Heroicon,
}

/// <summary>
/// A typed reference to an icon from a specific icon set.
/// Implicit conversions from <see cref="Lucide"/>, <see cref="Feather"/>,
/// and <see cref="Heroicon"/> enums allow passing enum values directly.
/// </summary>
public readonly record struct IconRef(IconSet Set, string Name)
{
    /// <summary>No icon — use as default when no icon is needed.</summary>
    public static readonly IconRef None = default;

    /// <summary>True when this represents no icon.</summary>
    public bool IsNone => string.IsNullOrEmpty(Name);

    // ── Implicit conversions (preferred usage) ───────────────────────────

    /// <summary>Implicitly converts a <see cref="Lucide"/> enum value to an <see cref="IconRef"/>.</summary>
    public static implicit operator IconRef(Lucide icon) =>
        icon == Editor.Shell.Lucide.None ? None : new(IconSet.Lucide, icon.ToIconName());

    /// <summary>Implicitly converts a <see cref="Feather"/> enum value to an <see cref="IconRef"/>.</summary>
    public static implicit operator IconRef(Feather icon) =>
        icon == Editor.Shell.Feather.None ? None : new(IconSet.Feather, icon.ToIconName());

    /// <summary>Implicitly converts a <see cref="Heroicon"/> enum value to an <see cref="IconRef"/>.</summary>
    public static implicit operator IconRef(Heroicon icon) =>
        icon == Editor.Shell.Heroicon.None ? None : new(IconSet.Heroicon, icon.ToIconName());

    // ── String fallback (for custom / unlisted icons) ────────────────────

    /// <summary>Creates an icon reference from a string name and icon set.</summary>
    public static IconRef FromName(IconSet set, string name) => new(set, name);

    public override string ToString() => IsNone ? "" : $"{Set}:{Name}";
}



