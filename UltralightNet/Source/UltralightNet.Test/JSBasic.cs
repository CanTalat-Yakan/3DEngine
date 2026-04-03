using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UltralightNet.JavaScript;

namespace UltralightNet.Test;

[Collection("Renderer")]
[Trait("Category", "JS")]
public sealed unsafe class JsBasic(RendererFixture fixture)
{
	private Renderer Renderer { get; } = fixture.Renderer;

	[Fact]
	public void GetMessageTest()
	{
		using var view = Renderer.CreateView(128, 128);
		var ctx = view.LockJsContext();

		using var name = JsString.CreateFromUtf16("GetMessage");
		var func = JsObject.MakeFunctionWithCallback(ctx, name.JsHandle, &GetMessage);
		JsObject.SetProperty(JsContext.GetGlobalObject(ctx), ctx,
			name.JsHandle, func);

		view.UnlockJsContext();

		string? result = view.EvaluateScript("GetMessage()", out string? exception);
		Assert.Equal("Hello from C#!", result);
		Assert.Empty(exception);
	}

	[UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
	private static JsValueRef GetMessage(JsContextRef ctx, JsObjectRef function, JsObjectRef thisObject,
		nuint argumentCount, JsValueRef* arguments, JsValueRef* exception)
	{
		{
			var thisObjectName = JsValue.ToStringCopy(thisObject, ctx);
			using var wrappedThisObjectName = JsString.FromHandle(thisObjectName, true);
			Assert.Equal("[object Window]", wrappedThisObjectName.ToString());
		}
		Assert.Equal((nuint)0, argumentCount); // (even though 'argumentCount' is 0, 'arguments' itself may not be null)

		using var jsString = JsString.CreateFromUtf16("Hello from C#!");
		var value = JsValue.MakeString(ctx, jsString.JsHandle);
		return value;
	}
}
