using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

using Engine.Components;
using Engine.Editor;
using Engine.Utilities;
using static Editor.Controller.Helper;

namespace Editor.Controller;

internal partial class Viewport
{
    public TextBlock Profile;
    public Grid Content;

    private ModelView.Viewport _viewport;

    public Viewport(ModelView.Viewport viewport, Grid content)
    {
        // Set the Viewport and Main content variable.
        _viewport = viewport;
        Content = content;

        // Call the CreateViewPortSettings function to build the UI.
        CreateViewPortSettings();
    }

    private void CreateViewPortSettings()
    {
        // Initialize an array of UI elements to be positioned in the top-left corner of the main content.
        UIElement[] topLeft = new[]
        {
                CreateButton(CreateIcon(Symbol.Video),
                    StackInGridVertical(
                            CreateSlider(
                                ViewportController.Camera,
                                "FOV",
                                90, 40, 110)
                                .WrapInGridVertical("Field Of View"),
                            CreateNumberInput(ViewportController.Instance, "MovementSpeed", 1, 100)
                                .WrapInGridVertical("Movement Speed"))),
                CreateAppBarSeperator(),
                CreateComboBox(new[] { "Perspective", "Orthogonal" }),
                CreateComboBox(new[] { "Lit", "Unlit", "Wireframe", "Shaded Wireframe" }),
            };

        // Initialize an array of UI elements to be positioned in the top-right corner of the main content.
        UIElement[] topRight = new[]
        {
                CreateButton(CreateIcon("\xE946"), CreateTextFull(out Profile).WrapInGrid()),

                CreateAppBarSeperator(),

                CreateToggleButton(CreateIcon("\xEA80"), true),
                CreateToggleButton(CreateIcon("\xE81E"), true),

                CreateAppBarSeperator(),

                CreateButtonWithValue(
                    CreateIcon("\xE80A"), 10,
                    CreateNumberInput(ViewportController.Instance, "MovementSpeed", 10, 1, 100)
                        .WrapInGridVertical("Grid Snap")),
                CreateButtonWithValue(
                    CreateIcon(Symbol.Rotate), 15, 
                    CreateNumberInput(ViewportController.Instance, "RotationSpeed", 15, 1, 90)
                        .WrapInGridVertical("Rotation Snap")),

                CreateAppBarSeperator(),

                CreateToggleButton(CreateIcon(Symbol.Globe), true),
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
