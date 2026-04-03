using System.Diagnostics;
using System.Threading;
using UltralightNet.Enums;
using UltralightNet.Structs;
using Xunit.Abstractions;

namespace UltralightNet.Test;

[Collection("Renderer")]
[Trait("Category", "Renderer")]
public sealed class ViewTest
{
	private readonly ITestOutputHelper _testOutputHelper;

	public ViewTest(RendererFixture fixture, ITestOutputHelper testOutputHelper)
	{
		_testOutputHelper = testOutputHelper;
		Renderer = fixture.Renderer;
	}

	private Renderer Renderer { get; }

	[Fact]
	[Trait("Network", "Required")]
	public void NetworkTest()
	{
		using var view = Renderer.CreateView(512, 512);

		Assert.Equal(512u, view.Width);
		Assert.Equal(512u, view.Height);

		var OnChangeTitle = false;
		var OnChangeURL = false;

		view.OnChangeTitle += title =>
		{
			Assert.Contains("GitHub", title);
			OnChangeTitle = true;
		};

		view.OnChangeUrl += url =>
		{
			Assert.Equal("https://github.com/", url);
			OnChangeURL = true;
		};

		view.Url = "https://github.com/";

		var sw = Stopwatch.StartNew();

		while (view.Url == "")
		{
			if (sw.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("Couldn't load page in 10 seconds.");

			Renderer.Update();
			Thread.Sleep(100);
		}

		Renderer.Render();

		Assert.Equal("https://github.com/", view.Url);
		Assert.Contains("GitHub", view.Title);
		Assert.True(OnChangeTitle);
		Assert.True(OnChangeURL);
	}

	[Fact]
	public void Html()
	{
		using var view = Renderer.CreateView(512, 512);
		view.LoadHtml("<html />");
	}

	[Fact]
	public void JsTest()
	{
		using var view = Renderer.CreateView(2, 2);
		Assert.Equal("3", view.EvaluateScript("1+2", out string exception));
		Assert.True(string.IsNullOrEmpty(exception));

		var called = false;
		view.OnAddConsoleMessage += (_, _, _, _, _, _) => called = true;
		view.EvaluateScript("console.log(123)", out _);

		Assert.True(called);
	}

	[Fact]
	public void EventTest()
	{
		using var view = Renderer.CreateView(256, 256);
		using var keyEvent = KeyEvent.Create(KeyEventType.Char, KeyEventModifiers.ShiftKey, 0, 0, "A", "A", false,
			false, false);
		view.FireKeyEvent(keyEvent);
		view.FireMouseEvent(new MouseEvent
			{ Type = MouseEventType.MouseDown, X = 100, Y = 100, Button = MouseEventButton.Left });
		view.FireScrollEvent(new ScrollEvent { Type = ScrollEventType.ByPage, DeltaX = 23, DeltaY = 123 });
	}

	[Fact]
	public void InspectorView()
	{
		using var view = Renderer.CreateView(256, 256);

		view.OnCreateInspectorView = (isLocal, inspectedUrl) =>
		{
			_testOutputHelper.WriteLine($"OnCreateInspectorView: {isLocal}, {inspectedUrl}");
			var inspectorView = Renderer.CreateView(256, 256);
			inspectorView.Url = inspectedUrl;
			Assert.NotNull(inspectorView);
			return inspectorView;
		};
		view.CreateLocalInspectorView();
	}
}
