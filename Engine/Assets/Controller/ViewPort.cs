using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Editor.ModelView;
using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using Windows.Foundation;
using Engine.Components;
using Engine.Editor;
using Microsoft.UI.Xaml.Data;

namespace Editor.Controller
{
    internal partial class ViewPort
    {
        public TextBlock Profile;
        public Grid GridMain;

        private ModelView.ViewPort _viewPort;

        public ViewPort(ModelView.ViewPort viewPort, Grid grid)
        {
            _viewPort = viewPort;
            GridMain = grid;

            CreateViewPortSettings();
        }

        private void CreateViewPortSettings()
        {
            UIElement[] topLeft = new[]
            {
                CreateButton(CreateIcon(Symbol.Video),
                    StackInGridVertical(
                            CreateSlider((float)Camera.s_FieldOfView, 40, 110, (s, e) => { Camera.s_FieldOfView = e.NewValue; }).WrapInGridVertical("Field Of View"),
                            CreateNumberInput(CameraController.s_MovementSpeed, 1, 100, (s, e) => { CameraController.s_MovementSpeed = (float)e.NewValue; }).WrapInGridVertical("Movement Speed"))),
                CreateAppBarSeperator(),
                CreateComboBox(new string[] { "Perspective", "Orthogonal" }),
                CreateComboBox(new string[] { "Lit", "Unlit", "Wireframe", "Shaded Wireframe" }),
            };

            GridMain.Children.Add(WrapInStackPanelDockTopLeft(topLeft));

            UIElement[] topRight = new[]
            {
                CreateButton(CreateIcon("\xE946"), CreateTextFull(out Profile).WrapInGrid()),
                CreateAppBarSeperator(),
                CreateToggleButton(CreateIcon("\xEA80"), true),
                CreateToggleButton(CreateIcon("\xE81E"), true),
                CreateAppBarSeperator(),
                CreateButtonWithValue(CreateIcon("\xE80A"), 10, CreateNumberInput(10, 1, 100, (s, e) => { CameraController.s_MovementSpeed = (float)e.NewValue; }).WrapInGridVertical("Grid Snap")),
                CreateButtonWithValue(CreateIcon(Symbol.Rotate), 15, CreateNumberInput(15, 1, 90, (s, e) => { CameraController.s_MovementSpeed = (float)e.NewValue; }).WrapInGridVertical("Rotation Snap")),
                CreateAppBarSeperator(),
                CreateToggleButton(CreateIcon(Symbol.Globe), true),
            };

            GridMain.Children.Add(WrapInStackPanelDockTopRight(topRight));
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
