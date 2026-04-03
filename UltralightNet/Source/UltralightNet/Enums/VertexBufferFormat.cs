namespace UltralightNet.Enums;

/// <summary>
/// Vertex buffer formats.
///
/// This enumeration describes the format of a vertex buffer.
/// </summary>
/// <remarks>
/// Identifiers start with 'Vbf' due to C# naming rules.
/// </remarks>
public enum VertexBufferFormat : byte
{
	/// <summary>
	/// Used for path rendering <br /><br />
	///     <see cref="float" />[2] <br />
	///     <see cref="byte" />[4] <br />
	///     <see cref="float" />[2] <br />
	/// </summary>
	Vbf2F4Ub2F,

	/// <summary>
	/// Used for quad rendering <br /><br />
	///     <see cref="float" />[2] <br />
	///     <see cref="byte" />[4] <br />
	///     <see cref="float" />[2] <br />
	///     <see cref="float" />[2] <br />
	///     <see cref="float" />[4] <br />
	///     <see cref="float" />[4] <br />
	///     <see cref="float" />[4] <br />
	///     <see cref="float" />[4] <br />
	///     <see cref="float" />[4] <br />
	///     <see cref="float" />[4] <br />
	///     <see cref="float" />[4] <br />
	/// </summary>
	Vbf2F4Ub2F2F28F
}
