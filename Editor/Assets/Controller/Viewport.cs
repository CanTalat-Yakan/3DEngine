using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using SharpGen.Runtime;
using System;
using WinUIEx;

using Engine.Editor;
using Engine.Utilities;

using static Editor.Controller.Helper;

namespace Editor.Controller;

internal partial class Viewport(ModelView.Viewport viewport, Grid content)
{
    public TextBlock Profile;
    public Grid Content = content;

    private ModelView.Viewport _viewport = viewport;

    public void InitializeEngineCore(Renderer renderer, out Engine.Core engineCore)
    {
        var hWnd = (Application.Current as App)?.Window.GetWindowHandle();
        engineCore = new Engine.Core(renderer, hWnd.Value, Files.AssetsPath);

        engineCore.Renderer.Data.SetVsync(false);
        engineCore.Renderer.Data.SetSuperSample(true);

        engineCore.OnInitialize += (s, e) =>
        {
            Binding.SetRendererBinding();

            CreateViewportSettings();
        };

        engineCore.OnRender += (s, e) =>
        {
            Binding.Update();
            Output.Log(Engine.Output.DequeueLog());

            _viewport._engineCore.SetPlayMode(
                Main.Instance.PlayerControl.PlayMode == PlayMode.Playing);
            _viewport._engineCore.SetPlayModeStarted(
                Main.Instance.PlayerControl.CheckPlayModeStarted());

            Profile.Text = Engine.Profiler.GetString();
        };

        engineCore.OnDispose += (s, e) =>
            Binding.Dispose();
    }

    public void InitializeRenderer(out Renderer renderer, SwapChainPanel swapChainPanel)
    {
        renderer = new();

        // Gets the native object for the SwapChainPanel control.
        using (var nativeObject = ComObject.As<Vortice.WinUI.ISwapChainPanelNative2>(swapChainPanel))
        {
            var result = nativeObject.SetSwapChain(renderer.SwapChain);
            if (result.Failure)
                throw new Exception(result.Description);
        }
    }

    public void CreateViewportSettings()
    {
        // Initialize an array of UI elements to be positioned in the top-left corner of the main content.
        UIElement[] topLeft = new[]
        {
                CreateFlyoutButton(
                    CreateIcon(Symbol.Video),
                    StackInGridVertical(
                        CreateSlider(
                            ViewportController.Instance?.Entity.ID,
                            ViewportController.Camera, "FOV",
                            90, 40, 110)
                            .WrapInGridVertical("Field Of View"),
                        CreateNumberInput(
                            ViewportController.Instance?.Entity.ID,
                            ViewportController.Instance, "_movementSpeed",
                            1, 100)
                            .WrapInGridVertical("Movement Speed"))),
                CreateAppBarSeperator(),
                CreateComboBox(new[] { "Perspective", "Orthogonal" }),
                CreateComboBox(new[] { "Lit", "Unlit", "Wireframe", "Shaded Wireframe" })
        };

        // Initialize an array of UI elements to be positioned in the top-right corner of the main content.
        UIElement[] topRight = new[]
        {
                CreateFlyoutButton(CreateIcon("\xE946"), CreateTextFull(out Profile).WrapInGrid()),

                CreateAppBarSeperator(),

                CreateToggleButton(CreateIcon("\xEA80"), true),
                CreateToggleButton(CreateIcon("\xE81E"), true),

                CreateAppBarSeperator(),

                CreateFlyoutButtonWithValue(
                    CreateIcon("\xE80A"), 10,
                    CreateNumberInput(
                        ViewportController.Instance?.Entity.ID,
                        ViewportController.Instance, "_movementSpeed",
                        10, 1, 100)
                        .WrapInGridVertical("Grid Snap")),
                CreateFlyoutButtonWithValue(
                    CreateIcon(Symbol.Rotate), 15,
                    CreateNumberInput(
                        ViewportController.Instance?.Entity.ID,
                        ViewportController.Instance, "_rotationSpeed",
                        15, 1, 90)
                        .WrapInGridVertical("Rotation Snap")),

                CreateAppBarSeperator(),

                CreateToggleButton(CreateIcon(Symbol.Globe), true)
        };

        // Add the top-left and top-right UI elements to the main content.
        Content.Children.Add(WrapInStackPanelDockTopLeft(topLeft));
        Content.Children.Add(WrapInStackPanelDockTopRight(topRight));
    }
}

internal partial class Viewport
{
    private StackPanel WrapInStackPanelDockTopLeft(params UIElement[] content)
    {
        StackPanel stack = new()
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.LeftToRight,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(32, 8, 8, 8)
        };

        foreach (var item in content)
            stack.Children.Add(item);

        return stack;
    }

    private StackPanel WrapInStackPanelDockTopRight(params UIElement[] content)
    {
        StackPanel stack = new()
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.RightToLeft,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(8, 8, 32, 8)
        };

        foreach (var item in content)
            stack.Children.Add(item);

        return stack;
    }

    private StackPanel WrapInStackPanelDockBottomLeft(params UIElement[] content)
    {
        StackPanel stack = new()
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.LeftToRight,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(32, 8, 8, 8)
        };

        foreach (var item in content)
            stack.Children.Add(item);

        return stack;
    }

    private StackPanel WrapInStackPanelDockBottomRight(params UIElement[] content)
    {
        StackPanel stack = new()
        {
            Orientation = Orientation.Horizontal,
            FlowDirection = FlowDirection.RightToLeft,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(8, 8, 32, 8)
        };

        foreach (var item in content)
            stack.Children.Add(item);

        return stack;
    }
}
