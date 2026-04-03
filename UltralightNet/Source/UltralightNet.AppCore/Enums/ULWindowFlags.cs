using System;

namespace UltralightNet.AppCore;

[Flags]
public enum ULWindowFlags
{
	Borderless = 1 << 0,
	Titled = 1 << 1,
	Resizable = 1 << 2,
	Maximizable = 1 << 3,
	Hidden = 1 << 4
}
