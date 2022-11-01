using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using Windows.Storage.Streams;
using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using Expander = Microsoft.UI.Xaml.Controls.Expander;
using ExpandDirection = Microsoft.UI.Xaml.Controls.ExpandDirection;
using System.Xml.Linq;
using Vortice.Direct2D1;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Shapes;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Editor.Controls
{
    internal class PropertiesController
    {
        internal async void SelectImage(Image _image, TextBlock _path)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage() { DecodePixelHeight = 48, DecodePixelWidth = 48 };
                    await bitmapImage.SetSourceAsync(fileStream);
                    _image.Source = bitmapImage;
                }
                _path.Text = file.Name;
            }
        }

        internal async void SelectFile(TextBlock _path)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.FileTypeFilter.Add("*");

            Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                _path.Text = file.Name;
            }
        }


        internal Grid WrapProperty(string _header, params UIElement[] _content)
        {
            Grid grid = new Grid();
            StackPanel stack = new StackPanel() { Orientation = Orientation.Horizontal };
            TextBlock header = new TextBlock() { Text = _header + ":", Width = 80, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            foreach (var item in _content)
                stack.Children.Add(item);
            grid.Children.Add(stack);

            return grid;
        }

        internal Grid CreateNumberInput(string _header = "Float", float _number = 0)
        {
            NumberBox numInput = new NumberBox() { Value = _number, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, MaxWidth = 200 };

            return WrapProperty(_header, numInput);
        }

        internal Grid CreateTextInput(string _header = "String", string _text = "Example")
        {
            TextBox textInput = new TextBox() { Text = _text, MaxWidth = 200 };

            return WrapProperty(_header, textInput);
        }

        internal Grid CreateVec2Input(string _header = "Vector2", float _number = 0, float _number2 = 0)
        {
            NumberBox numInput = new NumberBox() { Value = _number, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 98 };
            NumberBox num2Input = new NumberBox() { Value = _number2, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 98 };

            return WrapProperty(_header, numInput, num2Input);
        }

        internal Grid CreateVec3Input(string _header = "Vector3", float _number = 0, float _number2 = 0, float _number3 = 0)
        {
            NumberBox numInput = new NumberBox() { Value = _number, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num2Input = new NumberBox() { Value = _number2, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num3Input = new NumberBox() { Value = _number3, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            return WrapProperty(_header, numInput, num2Input, num3Input);
        }

        internal Grid CreateSlider(string _header = "Slider", float _value = 0)
        {
            Slider numInput = new Slider() { Value = _value, Width = 200, Margin = new Thickness(0, 0, 0, -5.5) };
            TextBlock numPreview = new TextBlock() { Padding = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            return WrapProperty(_header, numInput, numPreview);
        }

        internal Grid CreateBool(string _header = "Bool", bool _value = false)
        {
            CheckBox check = new CheckBox() { IsChecked = _value, Margin = new Thickness(0, 0, 0, -5.5) };

            return WrapProperty(_header, check);
        }

        internal Grid CreateTextureSlot(string _header = "Texture")
        {
            Grid container = new Grid() { Width = 48, Height = 48 };
            Image img = new Image() { Stretch = Stretch.UniformToFill };
            Button button = new Button() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            TextBlock path = new TextBlock() { Text = "None", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            container.Children.Add(img);
            container.Children.Add(button);

            return WrapProperty(_header, container, path);
        }

        internal Grid CreateReferenceSlot(string _header = "Reference")
        {
            Button button = new Button() { Content = "..." };
            TextBlock reference = new TextBlock() { Text = "None (type)", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            return WrapProperty(_header, button, reference);
        }

        internal Grid CreateEvent(string _header = "Button", string _button = "Event")
        {
            Button button = new Button() { Content = _button };

            return WrapProperty(_header, button);
        }

        internal Grid CreateColorButton(string _header = "Color", byte r = 0, byte g = 0, byte b = 0, byte a = 0)
        {
            Windows.UI.Color col = new Windows.UI.Color();
            col.R = r; col.G = g; col.B = b; col.A = a;
            ColorPickerButton colbutton = new ColorPickerButton() { SelectedColor = col };

            var stylee = new Style { TargetType = typeof(ColorPicker) };
            stylee.Setters.Add(new Setter(ColorPicker.ColorSpectrumShapeProperty, ColorSpectrumShape.Ring));
            stylee.Setters.Add(new Setter(ColorPicker.IsAlphaEnabledProperty, true));
            stylee.Setters.Add(new Setter(ColorPicker.IsHexInputVisibleProperty, true));
            colbutton.ColorPickerStyle = stylee;

            return WrapProperty(_header, colbutton);
        }



        internal Grid CreateHeader(string _header = "Header")
        {
            Grid grid = new Grid();
            TextBlock header = new TextBlock() { Text = _header, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 0) };

            grid.Children.Add(header);

            return grid;
        }

        internal Grid CreateSpacer()
        {
            Grid grid = new Grid() { Height = 10 };

            return grid;
        }

        internal Grid WrapExpander(Grid _content, string _header = "Expander")
        {
            Grid grid = new Grid();
            Expander expander = new Expander() { Header = _header, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };

            expander.Content = _content;
            grid.Children.Add(expander);

            return grid;
        }

        internal Grid CreateScript(string _header = "ExampleScript", params Grid[] _properties)
        {
            Grid grid = new Grid() { Margin = new Thickness(0,0,0,2)};
            StackPanel stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 10 };
            Expander expander = new Expander() { Header = _header, ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
            expander.Header = new ToggleButton() { Content = _header, IsChecked = true };

            foreach (var item in _properties)
                stack.Children.Add(item);

            expander.Content = stack;
            grid.Children.Add(expander);

            return grid;
        }
    }
}
