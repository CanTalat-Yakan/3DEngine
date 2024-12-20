﻿using System;
using WinRT.Interop;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

using SharpGen.Runtime;

using Engine;
using Engine.Editor;
using Engine.Essentials;
using Engine.Utilities;

using static Editor.Controller.Helper;

namespace Editor.Controller;

internal sealed partial class Viewport(Grid content)
{
    public Grid Content = content;

    private TextBlock _profiler;

    public void Initialize(out Engine.Kernel engineKernel, SwapChainPanel swapChainPanel)
    {
        EditorState.EditorBuild = true;

        var hwnd = WindowNative.GetWindowHandle((Application.Current as App)?.Window as MainWindow);

        var config = Config.GetDefault();
        config.SetVSync(PresentInterval.Immediate);
        config.SetMSAA(MultiSample.x2);
        config.SetResolutionScale(1);
        config.GUI = false;

        engineKernel = new(config);
        engineKernel.Initialize(hwnd, new(1024, 1024), win32Window: false, assetsPath: Files.AssetsPath);

        // Gets the native object for the SwapChainPanel control.
        using (var nativeObject = ComObject.As<Vortice.WinUI.ISwapChainPanelNative2>(swapChainPanel))
        {
            var result = nativeObject.SetSwapChain(engineKernel.Context.GraphicsDevice.SwapChain);
            if (result.Failure)
                throw new Exception(result.Description);
        }

        engineKernel.OnInitialize += () =>
        {
            Binding.SetRendererBindings();

            CreateViewportSettings();
        };

        engineKernel.OnRender += () =>
        {
            Binding.Update();
            Output.Log(Engine.Utilities.Output.DequeueLog());

            EditorState.SetPlayMode(
                Main.Instance.PlayerControl.PlayMode == PlayMode.Playing);
            EditorState.SetPlayModeStarted(
                Main.Instance.PlayerControl.CheckPlayModeStarted());

            if (Time.OnFrame)
                _profiler.Text = Profiler.GetAdditionalString();
        };

        engineKernel.OnGUI += () =>
        {

        };

        engineKernel.OnDispose += Binding.Dispose;
    }

    public void CreateViewportSettings()
    {
        // Initialize an array of UI elements to be positioned in the top-left corner of the main content.
        UIElement[] topLeft =
        [
            CreateFlyoutButton(
                CreateIcon(Symbol.Video),
                StackInGridVertical(
                    CreateSlider(null,
                        ViewportController.Camera, "FOV",
                        90, 40, 110)
                        .WrapInGridVertical("Field Of View"),
                    CreateNumberInput(null,
                        ViewportController.Instance, "_movementSpeed",
                        1, 100)
                        .WrapInGridVertical("Movement Speed"))),
            CreateAppBarSeperator(),
            CreateComboBox(typeof(CameraProjection), null,
                Engine.Kernel.Instance.Config, "CameraProjection"),
            CreateComboBox(typeof(RenderMode), null,
                Engine.Kernel.Instance.Config, "RenderMode")
        ];

        // Initialize an array of UI elements to be positioned in the top-right corner of the main content.
        UIElement[] topRight =
        [
            CreateFlyoutButton(CreateIcon("\xE946"), CreateTextFull(out _profiler).WrapInGrid()).AddToolTip("Profile"),

            CreateAppBarSeperator(),

            CreateToggleButton(CreateIcon("\xEA80"), true).AddToolTip("Snap Rotation"),
            CreateToggleButton(CreateIcon("\xE81E"), true).AddToolTip("Snap Position"),

            CreateAppBarSeperator(),

            CreateFlyoutButtonWithValue(
                CreateIcon("\xE80A"), 10,
                CreateNumberInput(null,
                    ViewportController.Instance, "_movementSpeed",
                    10, 1, 100)
                    .WrapInGridVertical("Grid Snap")).AddToolTip("Grid Snap"),
            CreateFlyoutButtonWithValue(
                CreateIcon(Symbol.Rotate), 15,
                CreateNumberInput(null,
                    ViewportController.Instance, "_rotationSpeed",
                    15, 1, 90)
                    .WrapInGridVertical("Rotation Snap")).AddToolTip("Rotation Snap"),

            CreateAppBarSeperator(),

            CreateToggleButton(CreateIcon(Symbol.Globe), true).AddToolTip("Show Gizmos")
        ];

        // Add the top-left and top-right UI elements to the main content.
        Content.Children.Add(WrapInStackPanelDockTopLeft(topLeft));
        Content.Children.Add(WrapInStackPanelDockTopRight(topRight));
    }
}

internal sealed partial class Viewport
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
            Margin = new(32, 8, 8, 8)
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
            Margin = new(8, 8, 32, 8)
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
            Margin = new(32, 8, 8, 8)
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
            Margin = new(8, 8, 32, 8)
        };

        foreach (var item in content)
            stack.Children.Add(item);

        return stack;
    }
}
