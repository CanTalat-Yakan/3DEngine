namespace Engine;

/// <summary>Marks a struct as an ECS Behavior; methods with stage attributes will be scheduled by the generator.</summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class BehaviorAttribute : Attribute
{
}

/// <summary>Runs once during app startup, before the window loop begins.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnStartupAttribute : Attribute
{
}

/// <summary>Runs at the beginning of each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFirstAttribute : Attribute
{
}

/// <summary>Runs before Update each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnPreUpdateAttribute : Attribute
{
}

/// <summary>Runs during the main update stage each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnUpdateAttribute : Attribute
{
}

/// <summary>Runs after Update each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnPostUpdateAttribute : Attribute
{
}

/// <summary>Runs during the render stage each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnRenderAttribute : Attribute
{
}

/// <summary>Runs at the very end of each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnLastAttribute : Attribute
{
}

/// <summary>Runs once during app cleanup, after the window loop ends.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnCleanupAttribute : Attribute
{
}

/// <summary>Filter: schedule only for entities that also have all listed component types.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class WithAttribute : Attribute
{
    public Type[] Types { get; }
    public WithAttribute(params Type[] types) => Types = types;
}

/// <summary>Filter: skip entities that have any of the listed component types.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class WithoutAttribute : Attribute
{
    public Type[] Types { get; }
    public WithoutAttribute(params Type[] types) => Types = types;
}

/// <summary>Filter: run only if any of the listed component types changed this frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ChangedAttribute : Attribute
{
    public Type[] Types { get; }
    public ChangedAttribute(params Type[] types) => Types = types;
}