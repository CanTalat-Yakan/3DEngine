namespace Engine;

/// <summary>Types of data that can flow through render graph slot edges.</summary>
public enum SlotType
{
    /// <summary>Default / unset - no value has been assigned to this slot.</summary>
    None = 0,
    /// <summary>A GPU image view (texture).</summary>
    TextureView,
    /// <summary>A GPU sampler.</summary>
    Sampler,
    /// <summary>A GPU buffer.</summary>
    Buffer,
    /// <summary>An entity identifier.</summary>
    Entity
}

/// <summary>Declares a named typed slot on a render graph node.</summary>
/// <param name="Name">Human-readable slot name.</param>
/// <param name="Type">The type of data this slot carries.</param>
public readonly record struct SlotInfo(string Name, SlotType Type);

/// <summary>Runtime value carried through a slot edge between render graph nodes.</summary>
public readonly struct SlotValue
{
    private readonly object? _value;

    /// <summary>The type of data stored in this slot value.</summary>
    public readonly SlotType Type;

    private SlotValue(object? value, SlotType type) { _value = value; Type = type; }

    /// <summary>Creates a slot value wrapping an image view.</summary>
    public static SlotValue TextureView(IImageView view) => new(view, SlotType.TextureView);

    /// <summary>Creates a slot value wrapping a sampler.</summary>
    public static SlotValue Sampler(ISampler sampler) => new(sampler, SlotType.Sampler);

    /// <summary>Creates a slot value wrapping a buffer.</summary>
    public static SlotValue Buffer(IBuffer buffer) => new(buffer, SlotType.Buffer);

    /// <summary>Creates a slot value wrapping an entity identifier.</summary>
    public static SlotValue Entity(int entity) => new(entity, SlotType.Entity);

    /// <summary>Extracts the stored image view. Throws if the type is wrong.</summary>
    public IImageView AsTextureView() => (IImageView)_value!;

    /// <summary>Extracts the stored sampler. Throws if the type is wrong.</summary>
    public ISampler AsSampler() => (ISampler)_value!;

    /// <summary>Extracts the stored buffer. Throws if the type is wrong.</summary>
    public IBuffer AsBuffer() => (IBuffer)_value!;

    /// <summary>Extracts the stored entity identifier. Throws if the type is wrong.</summary>
    public int AsEntity() => (int)_value!;
}

