using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using Expander = Microsoft.UI.Xaml.Controls.Expander;
using ExpandDirection = Microsoft.UI.Xaml.Controls.ExpandDirection;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;

namespace Editor.Controls
{
    internal class PropertiesController
    {
        public async void SelectImageAsync(Image image, TextBlock path)
        {
            var picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
                FileTypeFilter = { ".jpg", ".jpeg", ".png" }
            };

            // Make sure to get the HWND from a Window object,
            // pass a Window reference to GetWindowHandle.
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)?.Window as MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage() { DecodePixelHeight = 48, DecodePixelWidth = 48 };
                    await bitmapImage.SetSourceAsync(fileStream);
                    image.Source = bitmapImage;
                }
                path.Text = file.Name;
            }
        }

        public async void SelectFileAsync(TextBlock path)
        {
            var picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.Desktop,
                FileTypeFilter = { "*" }
            };

            // Make sure to get the HWND from a Window object,
            // pass a Window reference to GetWindowHandle.
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)?.Window as MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                path.Text = file.Name;
            }
        }

        public Grid WrapProperty(string s, params UIElement[] _content)
        {
            Grid grid = new Grid();
            StackPanel stack = new StackPanel() { Orientation = Orientation.Horizontal };
            TextBlock header = new TextBlock() { Text = s + ":", Width = 80, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            foreach (var item in _content)
                stack.Children.Add(item);
            grid.Children.Add(stack);

            return grid;
        }

        public Grid CreateNumberInput(string s = "Float", float i = 0)
        {
            NumberBox numInput = new NumberBox() { Value = i, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, MaxWidth = 200 };

            return WrapProperty(s, numInput);
        }

        public Grid CreateTextInput(string s = "String", string placeholder = "Example")
        {
            TextBox textInput = new TextBox() { Text = placeholder, MaxWidth = 200 };

            return WrapProperty(s, textInput);
        }

        public Grid CreateVec2Input(string s = "Vector2", float x = 0, float y = 0)
        {
            NumberBox numInput = new NumberBox() { Value = x, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 98 };
            NumberBox num2Input = new NumberBox() { Value = y, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 98 };

            return WrapProperty(s, numInput, num2Input);
        }

        public Grid CreateVec3Input(string s = "Vector3", float x = 0, float y = 0, float z = 0)
        {
            NumberBox numInput = new NumberBox() { Value = x, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num2Input = new NumberBox() { Value = y, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num3Input = new NumberBox() { Value = z, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            return WrapProperty(s, numInput, num2Input, num3Input);
        }

        public Grid CreateSlider(string s = "Slider", float i = 0)
        {
            Slider numInput = new Slider() { Value = i, Width = 200, Margin = new Thickness(0, 0, 0, -5.5) };
            TextBlock numPreview = new TextBlock() { Padding = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            return WrapProperty(s, numInput, numPreview);
        }

        public Grid CreateBool(string s = "Bool", bool b = false)
        {
            CheckBox check = new CheckBox() { IsChecked = b, Margin = new Thickness(0, 0, 0, -5.5) };

            return WrapProperty(s, check);
        }

        public Grid CreateTextureSlot(string s = "Texture")
        {
            Grid container = new Grid() { Width = 48, Height = 48 };
            Image img = new Image() { Stretch = Stretch.UniformToFill };
            Button button = new Button() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            TextBlock path = new TextBlock() { Text = "None", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            container.Children.Add(img);
            container.Children.Add(button);

            return WrapProperty(s, container, path);
        }

        public Grid CreateReferenceSlot(string s = "Reference")
        {
            Button button = new Button() { Content = "..." };
            TextBlock reference = new TextBlock() { Text = "None (type)", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            return WrapProperty(s, button, reference);
        }

        public Grid CreateEvent(string s = "Button", string s2 = "Event")
        {
            Button button = new Button() { Content = s2 };

            return WrapProperty(s, button);
        }

        public Grid CreateColorButton(string s = "Color", byte r = 0, byte g = 0, byte b = 0, byte a = 0)
        {
            Windows.UI.Color col = new Windows.UI.Color();
            col.R = r; col.G = g; col.B = b; col.A = a;
            ColorPickerButton colbutton = new ColorPickerButton() { SelectedColor = col };

            var stylee = new Style { TargetType = typeof(ColorPicker) };
            stylee.Setters.Add(new Setter(ColorPicker.ColorSpectrumShapeProperty, ColorSpectrumShape.Ring));
            stylee.Setters.Add(new Setter(ColorPicker.IsAlphaEnabledProperty, true));
            stylee.Setters.Add(new Setter(ColorPicker.IsHexInputVisibleProperty, true));
            colbutton.ColorPickerStyle = stylee;

            return WrapProperty(s, colbutton);
        }

        public Grid CreateHeader(string s = "Header")
        {
            Grid grid = new Grid();
            TextBlock header = new TextBlock() { Text = s, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 0) };

            grid.Children.Add(header);

            return grid;
        }

        public Grid CreateSpacer()
        {
            Grid grid = new Grid() { Height = 10 };

            return grid;
        }

        public Grid WrapExpander(Grid content, string s = "Expander")
        {
            Grid grid = new Grid();
            Expander expander = new Expander() { Header = s, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };

            expander.Content = content;
            grid.Children.Add(expander);

            return grid;
        }

        public Grid CreateScript(string s = "ExampleScript", params Grid[] properties)
        {
            Grid grid = new Grid() { Margin = new Thickness(0,0,0,2)};
            StackPanel stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 10 };
            Expander expander = new Expander() { Header = s, ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
            expander.Header = new ToggleButton() { Content = s, IsChecked = true };

            foreach (var item in properties)
                stack.Children.Add(item);

            expander.Content = stack;
            grid.Children.Add(expander);

            return grid;
        }
    }
}
