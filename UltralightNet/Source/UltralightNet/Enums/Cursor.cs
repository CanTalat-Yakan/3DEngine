namespace UltralightNet.Enums;

/// <summary>
/// Cursor types, see <see cref="View.OnChangeCursor"/>
/// </summary>
public enum Cursor // CAPI_Defines.h - no type
{
	Pointer,
	Cross,
	Hand,
	// ReSharper disable once InconsistentNaming
	IBeam,
	Wait,
	Help,
	EastResize,
	NorthResize,
	NorthEastResize,
	NorthWestResize,
	SouthResize,
	SouthEastResize,
	SouthWestResize,
	WestResize,
	NorthSouthResize,
	EastWestResize,
	NorthEastSouthWestResize,
	NorthWestSouthEastResize,
	ColumnResize,
	RowResize,
	MiddlePanning,
	EastPanning,
	NorthPanning,
	NorthEastPanning,
	NorthWestPanning,
	SouthPanning,
	SouthEastPanning,
	SouthWestPanning,
	WestPanning,
	Move,
	VerticalText,
	Cell,
	ContextMenu,
	Alias,
	Progress,
	NoDrop,
	Copy,
	None,
	NotAllowed,
	ZoomIn,
	ZoomOut,
	Grab,
	Grabbing,
	Custom
}
