using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Engine.Components;
using Engine.Editor;
using Engine.Utilities;

namespace Editor.Controller
{
    internal partial class ViewPort
    {
        public TextBlock Profile;
        public Grid Content;

        private ModelView.ViewPort _viewPort;

        public ViewPort(ModelView.ViewPort viewPort, Grid content)
        {
            // Set the Viewport and Maincontent variable.
            _viewPort = viewPort;
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
                            CreateSlider(90, 40, 110,
                            (s, e) => { SceneManager.Scene.EntitytManager.GetFromTag("SceneCamera").GetComponent<Camera>().FieldOfView= (float)e.NewValue; }).WrapInGridVertical("Field Of View"),
                            CreateNumberInput(CameraController.MovementSpeed, 1, 100, (s, e) => { CameraController.MovementSpeed = (float)e.NewValue; }).WrapInGridVertical("Movement Speed"))),
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
                CreateButtonWithValue(CreateIcon("\xE80A"), 10, CreateNumberInput(10, 1, 100, (s, e) => { CameraController.MovementSpeed = (float)e.NewValue; }).WrapInGridVertical("Grid Snap")),
                CreateButtonWithValue(CreateIcon(Symbol.Rotate), 15, CreateNumberInput(15, 1, 90, (s, e) => { CameraController.MovementSpeed = (float)e.NewValue; }).WrapInGridVertical("Rotation Snap")),
                CreateAppBarSeperator(),
                CreateToggleButton(CreateIcon(Symbol.Globe), true),
            };

            // Add the top-left and top-right UI elements to the main content.
            Content.Children.Add(WrapInStackPanelDockTopLeft(topLeft));
            Content.Children.Add(WrapInStackPanelDockTopRight(topRight));
        }
    }

    internal partial class ViewPort : Controller.Helper
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
}
