using UltralightNet.Structs;

namespace UltralightNet.Test;

public class UlRectsTests
{
	[Fact]
	public void EqualityF()
	{
		UlRect rect1 = new() { Left = 99, Top = 123, Right = 1215623, Bottom = -12.63f };
		UlRect rect2 = new() { Left = 99, Top = 123, Right = 1215623, Bottom = -12.63f };

		Assert.Equal(rect1, rect2);
		Assert.True(rect1 == rect2);
		Assert.False(rect1 != rect2);
		Assert.True(rect1.Equals(rect2));
	}

	[Fact]
	public void EqualityI()
	{
		UlIntRect rect1 = new() { Left = 99, Top = 123, Right = 1215623, Bottom = -12 };
		UlIntRect rect2 = new() { Left = 99, Top = 123, Right = 1215623, Bottom = -12 };

		Assert.Equal(rect1, rect2);
		Assert.True(rect1 == rect2);
		Assert.False(rect1 != rect2);
		Assert.True(rect1.Equals(rect2));
	}

	[Fact]
	public void ConversionToInt()
	{
		UlRect rect = new() { Left = -10, Top = 10, Right = 17, Bottom = 20 };
		var iRect = (UlIntRect)rect;
		Assert.Equal(-10, iRect.Left);
		Assert.Equal(10, iRect.Top);
		Assert.Equal(17, iRect.Right);
		Assert.Equal(20, iRect.Bottom);
	}

	[Fact]
	public void ConversionToFloat()
	{
		UlIntRect iRect = new() { Left = -10, Top = 10, Right = 17, Bottom = 20 };
		var rect = (UlRect)iRect;
		Assert.Equal(-10, rect.Left);
		Assert.Equal(10, rect.Top);
		Assert.Equal(17, rect.Right);
		Assert.Equal(20, rect.Bottom);
	}
}
