using System.Collections.Generic;
using UltralightNet.JavaScript;

namespace UltralightNet.Test;

[Trait("Category", "JS")]
public unsafe class JsStringTest
{
	private const string TestString = "lorem ipsum, or something...";
	private static ReadOnlySpan<byte> TestStringUtf8 => "lorem ipsum, or something...\0"u8;

	public static IEnumerable<object[]> InvalidStrings()
	{
		yield return [""];
		yield return [null!];
	}

	[Fact]
	public void CreateFromCharSpan()
	{
		using var str = JsString.CreateFromUtf16(TestString.AsSpan());
		Assert.Equal(TestString.Length, (int)str.Length);
		Assert.NotEqual((nuint)0, (nuint)str.Utf16DataRaw);
		Assert.True(TestString.AsSpan().SequenceEqual(str.Utf16Data));
		Assert.Equal(TestString, str.ToString());
	}

	[Theory]
	[MemberData(nameof(InvalidStrings))]
	public void CreateFromEmptyCharSpan(string? testString)
	{
		using var str = JsString.CreateFromUtf16(testString.AsSpan());
		Assert.Equal((nuint)0, str.Length);
		Assert.Equal((nuint)0, (nuint)str.Utf16DataRaw);
		Assert.Equal(string.Empty, str.ToString());
	}

	[Fact]
	public void CreateFromByteSpan()
	{
		using var str = JsString.CreateFromUtf8NullTerminated(TestStringUtf8);
		Assert.Equal(TestString.Length, (int)str.Length);
		Assert.Equal(TestString, str.ToString());

		Assert.True(str.EqualsNullTerminatedUtf8(TestStringUtf8));

		Span<byte> bytes = stackalloc byte[(int)str.MaximumUtf8CStringSize];
		Assert.True(bytes.Length >= TestStringUtf8.Length);
		bytes.Fill(byte.MaxValue);
		var written = str.GetUtf8(bytes);
		Assert.Equal(TestStringUtf8.Length, (int)written);
		bytes[(int)written] = 0;
		bytes = bytes[..(int)written];
		Assert.True(TestStringUtf8.SequenceEqual(bytes));

		Assert.Throws<ArgumentException>("utf8", () => JsString.CreateFromUtf8NullTerminated(TestStringUtf8[..^1]));
		Assert.Throws<ArgumentException>("utf8", () => JsString.CreateFromUtf8NullTerminated(ReadOnlySpan<byte>.Empty));
	}

	[Fact]
	public void EqualityTests()
	{
		using var str = JsString.CreateFromUtf8NullTerminated(TestStringUtf8);
		using var str2 = JsString.CreateFromUtf8NullTerminated(TestStringUtf8);
		Assert.Equal(TestString, str.ToString());
		Assert.True(str.Equals(TestString));
		Assert.True(str.Equals(str));
		Assert.True(str == str2);

		Assert.False(str.Equals(null));
		Assert.False(str.Equals((JsString?)null));
		Assert.True(str != null);

		Assert.Throws<ArgumentException>("utf8", () => str.EqualsNullTerminatedUtf8(TestStringUtf8[..^1]));
		Assert.Throws<ArgumentException>("utf8", () => str.EqualsNullTerminatedUtf8(ReadOnlySpan<byte>.Empty));
	}
}
