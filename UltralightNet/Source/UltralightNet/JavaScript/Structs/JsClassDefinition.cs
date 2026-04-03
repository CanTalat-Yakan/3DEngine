// JSObjectRef.h

namespace UltralightNet.JavaScript.Structs;

// ReSharper disable once GrammarMistakeInComment
/// <summary>
/// This structure contains properties and callbacks that define a type of object. All fields other than the version field are optional. Any pointer may be NULL. <br/> <br/>
/// <see cref="Version"/> - The version number of this structure. The current version is 0.  <br/>
/// <see cref="Attributes"/> - A logically ORed set of JSClassAttributes to give to the class. <br/>
/// <see cref="ClassName"/> - A null-terminated UTF8 string containing the class's name. <br/>
/// <see cref="ParentClass"/> - A JSClass to set as the class's parent class. Pass NULL use the default object class. <br/>
/// <see cref="StaticValues"/> - A JSStaticValue array containing the class's statically declared value properties. Pass NULL to specify no statically declared value properties. The array must be terminated by a JSStaticValue whose name field is NULL. <br/>
/// <see cref="StaticFunctions"/> - A JsStaticFunction array containing the class's statically declared function properties. Pass NULL to specify no statically declared function properties. The array must be terminated by a JsStaticFunction whose name field is NULL. <br/>
/// <see cref="Initialize"/> - The callback invoked when an object is first created. Use this callback to initialize the object. <br/>
/// <see cref="Finalise"/> - The callback invoked when an object is finalized (prepared for garbage collection). Use this callback to release resources allocated for the object, and perform other cleanup. <br/>
/// <see cref="HasProperty"/> - The callback invoked when determining whether an object has a property. If this field is NULL, getProperty is called instead. The hasProperty callback enables optimization in cases where only a property's existence needs to be known, not its value, and computing its value is expensive.  <br/>
/// <see cref="GetProperty"/> - The callback invoked when getting a property's value. <br/>
/// <see cref="SetProperty"/> - The callback invoked when setting a property's value. <br/>
/// <see cref="DeleteProperty"/> - The callback invoked when deleting a property. <br/>
/// <see cref="GetPropertyNames"/> - The callback invoked when collecting the names of an object's properties. <br/>
/// <see cref="CallAsFunction"/> - The callback invoked when an object is called as a function. <br/>
/// <see cref="HasInstance"/> - The callback invoked when an object is used as the target of an 'instanceof' expression. <br/>
/// <see cref="CallAsConstructor"/> - The callback invoked when an object is used as a constructor in a 'new' expression. <br/>
/// <see cref="ConvertToType"/> - The callback invoked when converting an object to a particular JavaScript type. <br/>
/// <br/>
/// The StaticValues and StaticFunctions arrays are the simplest and most efficient means for vending custom properties. Statically declared properties automatically service requests like getProperty, setProperty, and getPropertyNames. Property access callbacks are required only to implement unusual properties, like array indexes, whose names are not known at compile-time.
/// <br/><br/>
/// If you named your getter function "GetX" and your setter function "SetX", you would declare a JSStaticValue array containing "X" like this:
/// <code>
/// JSStaticValue StaticValueArray[] = {
///     { "X", GetX, SetX, kJSPropertyAttributeNone },
///     { 0, 0, 0, 0 }
/// };
/// </code>
///
/// Standard JavaScript practice calls for storing function objects in prototypes, so they can be shared. The default JSClass created by JSClassCreate follows this idiom, instantiating objects with a shared, automatically generating prototype containing the class's function objects. The kJSClassAttributeNoAutomaticPrototype attribute specifies that a JSClass should not automatically generate such a prototype. The resulting JSClass instantiates objects with the default object prototype, and gives each instance object its own copy of the class's function objects.
///
/// A NULL callback specifies that the default object callback should substitute, except in the case of hasProperty, where it specifies that getProperty should substitute.
///
/// It is not possible to use JS subclassing with objects created from a class definition that sets callAsConstructor by default. Subclassing is supported via the JSObjectMakeConstructor function, however.
/// </summary>
public unsafe struct JsClassDefinition
{
	public int Version;
	public JsClassAttributes Attributes;

	public byte* ClassName;
	public JsClassRef ParentClass;

	public void* StaticValues;
	public void* StaticFunctions;
	public void* Initialize;
	public void* Finalise;
	public void* HasProperty;
	public void* GetProperty;
	public void* SetProperty;
	public void* DeleteProperty;
	public void* GetPropertyNames;
	public void* CallAsFunction;
	public void* CallAsConstructor;
	public void* HasInstance;
	public void* ConvertToType;

	public void* PrivateData;
}
