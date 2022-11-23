using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Editor.UserControls;
using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using Windows.Foundation;
using Engine.Components;
using Engine.Editor;

namespace Editor.Controller
{
    internal partial class ViewPortController
    {
        public TextBlock Profile;
        public Grid GridMain;

        private ViewPort _viewPort;

        public ViewPortController(ViewPort viewPort, Grid grid)
        {
            _viewPort = viewPort;
            GridMain = grid;

            CreateViewPortSettings();
        }

        private void CreateViewPortSettings()
        {
            UIElement[] topLeft = new[]
            {
                CreateComboBox(new string[] { "Perspective", "Orthogonal" }),
                CreateComboBox(new string[] { "Lit", "Unlit", "Wireframe", "Shaded Wireframe" }),
            };

            GridMain.Children.Add(WrapInStackPanelDockTopLeft(topLeft));

            UIElement[] topRight = new[]
            {
                CreateButton(CreateIcon("\xE946"), CreateTextFull(out Profile)),
                CreateButton(CreateIcon(Symbol.Video),
                    StackInGrid(
                            CreateSlider("Field Of View", (float)CameraComponent.s_FieldOfView, 40, 110, (s, e) => { CameraComponent.s_FieldOfView = e.NewValue; }),
                            CreateNumberInput("Movement Speed", CameraController.s_MovementSpeed, 1, 100, (s, e) => { CameraController.s_MovementSpeed = (float)e.NewValue; }))),
                CreateAppBarSeperator(),
                CreateToggleButton(CreateIcon("\xE706")),
                CreateToggleButton(CreateIcon("\xE81E")),
                CreateAppBarSeperator(),
                CreateButtonWithValue(CreateIcon("\xE80A"), 10, CreateNumberInput("Grid Interval", 10, 0.1f, 100, (s, e) => { CameraController.s_MovementSpeed = (float)e.NewValue; })),
                CreateButtonWithValue(CreateIcon(Symbol.Rotate), 15, CreateNumberInput("Movement Speed", 15, 1, 90, (s, e) => { CameraController.s_MovementSpeed = (float)e.NewValue; })),
            };

            GridMain.Children.Add(WrapInStackPanelDockTopRight(topRight));
        }
    }

    internal partial class ViewPortController
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

        private Grid StackInGrid(params UIElement[] content)
        {
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            StackPanel stack = new() { Orientation = Orientation.Vertical, FlowDirection = FlowDirection.LeftToRight };

            foreach (var item in content)
                stack.Children.Add(item);

            grid.Children.Add(stack);

            return grid;
        }

        private ComboBox CreateComboBox(params string[] items)
        {
            ComboBox comboBox = new ComboBox() { Height = 33, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };

            foreach (var item in items)
                comboBox.Items.Add(item);

            return comboBox;
        }

        private UIElement CreateToggleButton(Grid icon, bool _isChecked = false)
        {
            ToggleButton button = new() { Content = icon, IsChecked = _isChecked };

            return button;
        }

        private UIElement CreateButton(Grid icon, Grid content)
        {
            Button button = new() { Content = icon };
            button.Flyout = new Flyout() { Content = content, Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };

            return button;
        }

        private UIElement CreateButtonWithValue(Grid icon, float f, Grid content)
        {
            StackPanel stack = new() { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight, Spacing = 5 };
            Viewbox viewbox = new() { Width = 16, Height = 16 };
            viewbox.Child = icon;

            stack.Children.Add(viewbox);
            stack.Children.Add(new TextBlock() { Text = f.ToString() });

            Button button = new() { Content = stack, FlowDirection = FlowDirection.LeftToRight };
            button.Flyout = new Flyout() { Content = content, Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };

            return button;
        }

        private UIElement CreateAppBarSeperator()
        {
            AppBarSeparator seperator = new();

            return seperator;
        }

        private Grid WrapInField(string s, params UIElement[] content)
        {
            Grid grid = new Grid();
            StackPanel stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Stretch };
            TextBlock header = new TextBlock() { Text = s, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            foreach (var item in content)
                stack.Children.Add(item);
            grid.Children.Add(stack);

            return grid;
        }

        private Grid CreateSeperator()
        {
            Grid grid = new Grid();

            NavigationViewItemSeparator seperator = new NavigationViewItemSeparator() { Margin = new Thickness(10) };

            grid.Children.Add(seperator);

            return grid;
        }

        private Grid CreateText(string s = "String", string placeholder = "Example")
        {
            TextBlock textInput = new TextBlock() { Text = placeholder, MaxWidth = 200, TextWrapping = TextWrapping.Wrap };

            return WrapInField(s, textInput);
        }

        private Grid CreateTextFull(out TextBlock textBlock)
        {
            textBlock = new TextBlock();

            Grid grid = new Grid();
            grid.Children.Add(textBlock);

            return grid;
        }

        private Grid CreateTextFull(string s = "String")
        {
            TextBlock textInput = new TextBlock() { Text = s, Opacity = 0.5f, TextWrapping = TextWrapping.Wrap };

            Grid grid = new Grid();
            grid.Children.Add(textInput);

            return grid;
        }

        private Grid CreateNumberInput(string s = "Float", float f = 0, float min = float.MinValue, float max = float.MaxValue, TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs> onValueChanged = null)
        {
            NumberBox numInput = new NumberBox() { Value = f, Minimum = min, Maximum = max, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, MaxWidth = 200 };
            numInput.ValueChanged += onValueChanged;

            return WrapInField(s, numInput);
        }

        private Grid CreateSlider(string s = "Slider", float f = 0, float min = float.MinValue, float max = float.MaxValue, RangeBaseValueChangedEventHandler onValueChanged = null)
        {
            Slider numInput = new Slider() { Value = f, Minimum = min, Maximum = max, Width = 200, Margin = new Thickness(0, 0, 0, -5.5) };
            numInput.ValueChanged += onValueChanged;

            TextBlock numPreview = new TextBlock() { Padding = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            return WrapInField(s, numInput, numPreview);
        }

        private Grid CreateTextInput(string s = "String", string placeholder = "Example")
        {
            TextBox textInput = new TextBox() { Text = placeholder, MaxWidth = 200 };

            return WrapInField(s, textInput);
        }

        private Grid CreateVec2Input(string s = "Vector2", float x = 0, float y = 0)
        {
            NumberBox numInput = new NumberBox() { Value = x, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 98 };
            NumberBox num2Input = new NumberBox() { Value = y, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 98 };

            return WrapInField(s, numInput, num2Input);
        }

        private Grid CreateVec3Input(string s = "Vector3", float x = 0, float y = 0, float z = 0)
        {
            NumberBox numInput = new NumberBox() { Value = x, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num2Input = new NumberBox() { Value = y, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num3Input = new NumberBox() { Value = z, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            return WrapInField(s, numInput, num2Input, num3Input);
        }

        private Grid CreateBool(string s = "Bool", bool b = false)
        {
            CheckBox check = new CheckBox() { IsChecked = b, Margin = new Thickness(0, 0, 0, -5.5) };

            return WrapInField(s, check);
        }

        private Grid CreateEvent(string s = "Button", string s2 = "Event")
        {
            Button button = new Button() { Content = s2 };

            return WrapInField(s, button);
        }

        private Grid CreateColorButton(string s = "Color", byte r = 0, byte g = 0, byte b = 0, byte a = 0)
        {
            Windows.UI.Color col = new Windows.UI.Color();
            col.R = r; col.G = g; col.B = b; col.A = a;
            ColorPickerButton colbutton = new ColorPickerButton() { SelectedColor = col };

            var stylee = new Style { TargetType = typeof(ColorPicker) };
            stylee.Setters.Add(new Setter(ColorPicker.ColorSpectrumShapeProperty, ColorSpectrumShape.Ring));
            stylee.Setters.Add(new Setter(ColorPicker.IsAlphaEnabledProperty, true));
            stylee.Setters.Add(new Setter(ColorPicker.IsHexInputVisibleProperty, true));
            colbutton.ColorPickerStyle = stylee;

            return WrapInField(s, colbutton);
        }

        private Grid CreateHeader(string s = "Header")
        {
            Grid grid = new Grid();
            TextBlock header = new TextBlock() { Text = s, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 0) };

            grid.Children.Add(header);

            return grid;
        }

        private Grid CreateSpacer()
        {
            Grid grid = new Grid() { Height = 10 };

            return grid;
        }

        private Grid CreateButton(string s, TappedEventHandler tapped)
        {
            Grid grid = new Grid();

            Button button = new Button() { Content = s, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(10) };

            button.Tapped += tapped;

            AutoSuggestBox suggestBox = new AutoSuggestBox();

            //FlyoutBase kbase = new FlyoutBase();
            //button.Flyout = suggestBox;

            grid.Children.Add(button);

            return grid;
        }

        private Grid CreateIcon(string glyph)
        {
            Grid grid = new Grid();

            FontIcon icon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = glyph };

            grid.Children.Add(icon);

            return grid;
        }

        private Grid CreateIcon(Symbol symbol)
        {
            Grid grid = new Grid();

            SymbolIcon symbolIcon = new SymbolIcon() { Symbol = symbol };

            grid.Children.Add(symbolIcon);

            return grid;
        }
    }
}
