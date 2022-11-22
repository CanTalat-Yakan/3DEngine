using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using Expander = Microsoft.UI.Xaml.Controls.Expander;
using ExpandDirection = Microsoft.UI.Xaml.Controls.ExpandDirection;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using Engine.Utilities;
using Microsoft.UI;
using System.IO;
using Path = System.IO.Path;
using Editor.UserControls;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text;

namespace Editor.Controls
{
    internal class PropertiesController
    {
        private StackPanel _stackPanel;

        public PropertiesController(StackPanel stackPanel, object content)
        {
            _stackPanel = stackPanel;

            if (content is null)
                CreateEmptyMessage();
            else if (content.GetType() == typeof(Entity))
                CreateEntityProperties((Entity)content);
            else if (content.GetType() == typeof(string))
                CreateFilePreviewer((string)content);
        }

        public static void Clear() => MainController.Instance.LayoutControl.PropertiesRoot.Children.Clear();
        public static void Set(Properties properties) => MainController.Instance.LayoutControl.PropertiesRoot.Children.Add(properties);

        private void CreateEntityProperties(Entity entity)
        {
            var properties = new Grid[]
            {
                CreateBool("Is Acitve", true),
                CreateBool("Is Static"),
                CreateEnum("Tag", "Untagged", "MainCamera", "Respawn", "Player", "Finish", "GameController"),
                CreateEnum("Layer", "Default", "Transparent FX", "Ignore Raycast", "Water", "UI")
            };

            var transform = new Grid[]
            {
                CreateVec3InputTransform("Position", entity.Transform.Position.X, entity.Transform.Position.Y, entity.Transform.Position.Z),
                CreateVec3InputTransform("Rotation", entity.Transform.Rotation.X, entity.Transform.Rotation.Y, entity.Transform.Rotation.Z),
                CreateVec3InputTransform("Scale", entity.Transform.Scale.X, entity.Transform.Scale.Y, entity.Transform.Scale.Z)
            };

            var collection = new Grid[]
            {
                CreateColorButton(),
                CreateNumberInput(),
                CreateTextInput(),
                CreateVec2Input(),
                CreateVec3Input(),
                CreateSlider(),
                CreateBool(),
                CreateTextureSlot(),
                CreateReferenceSlot(),
                CreateHeader(),
                WrapInExpander(CreateEvent())
            };

            _stackPanel.Children.Add(CreateExpanderWithEditableHeader(entity.Name, properties));
            _stackPanel.Children.Add(CreateSeperator());
            _stackPanel.Children.Add(CreateExpander("Transform", transform));
            _stackPanel.Children.Add(CreateButton("Add Component", null));
            _stackPanel.Children.Add(CreateExpanderWithToggleButton("Example", collection));
            _stackPanel.Children.Add(CreateExpanderWithToggleButton("Another", CreateSpacer()));
        }

        private async void CreateFilePreviewer(string path)
        {
            FileInfo fileInfo = new FileInfo(path);

            var properties = new Grid[]
            {
                CreateText("File name", Path.GetFileNameWithoutExtension(path)),
                CreateText("File type", Path.GetExtension(path)),
                CreateText("File size", SizeSuffix(fileInfo.Length)),
                CreateSpacer(),
                CreateTextEqual("Creation time", fileInfo.CreationTime.ToShortDateString() + " " + fileInfo.CreationTime.ToShortTimeString() ),
                CreateTextEqual("Last access time", fileInfo.LastAccessTime.ToShortDateString() + " " + fileInfo.LastAccessTime.ToShortTimeString()),
                CreateTextEqual("Last update time", fileInfo.LastWriteTime.ToShortDateString() + " " + fileInfo.LastWriteTime.ToShortTimeString())
            };

            _stackPanel.Children.Add(CreateExpander(Path.GetFileName(path), properties));
            _stackPanel.Children.Add(CreateSeperator());
            _stackPanel.Children.Add(CreateButton("Open File", (s, e) =>
            {
                if (File.Exists(path))
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }));

            if (File.Exists(path))
                if (fileInfo.Extension == ".cs" 
                    || fileInfo.Extension == ".txt"
                    || fileInfo.Extension == ".usd"
                    || fileInfo.Extension == ".mat"
                    || fileInfo.Extension == ".hlsl")
                {
                    string[] lines = await File.ReadAllLinesAsync(path);
                    string joinedLines = string.Join("\n", lines);

                    Grid[] preview = new Grid[] { CreateTextFull(joinedLines) };

                    _stackPanel.Children.Add(CreateExpander("Preview", preview));
                }
        }

        private void CreateEmptyMessage()
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 20, 0, 0) };

            StackPanel stack = new StackPanel() { HorizontalAlignment = HorizontalAlignment.Center };

            TextBlock textBlock = new TextBlock() { Text = "Select a file or an entity", Opacity = 0.5f, HorizontalAlignment = HorizontalAlignment.Center };
            TextBlock textBlock2 = new TextBlock() { Text = "to view its properties.", Opacity = 0.5f, HorizontalAlignment = HorizontalAlignment.Center };

            stack.Children.Add(textBlock);
            stack.Children.Add(textBlock2);

            grid.Children.Add(stack);

            _stackPanel.Children.Add(grid);
        }

        private Grid WrapInField(string s, params UIElement[] _content)
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

        private Grid WrapInFieldEqual(string s, params UIElement[] _content)
        {
            Grid grid = new Grid();
            StackPanel stack = new StackPanel() { Orientation = Orientation.Horizontal };
            TextBlock header = new TextBlock() { Text = s + ":", Width = 160, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            foreach (var item in _content)
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

        private Grid CreateTextEqual(string s = "String", string placeholder = "Example")
        {
            TextBlock textInput = new TextBlock() { Text = placeholder, MaxWidth = 200 };

            return WrapInFieldEqual(s, textInput);
        }

        private Grid CreateTextFull(string s = "String")
        {
            TextBlock textInput = new TextBlock() { Text = s, Opacity = 0.5f, TextWrapping = TextWrapping.Wrap };

            Grid grid = new Grid();
            grid.Children.Add(textInput);

            return grid;
        }

        private Grid CreateNumberInput(string s = "Float", float i = 0)
        {
            NumberBox numInput = new NumberBox() { Value = i, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact, MaxWidth = 200 };

            return WrapInField(s, numInput);
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

        private Grid CreateVec3InputTransform(string s = "Vector3", float x = 0, float y = 0, float z = 0)
        {
            Rectangle rectangleR = new Rectangle() { Fill = new SolidColorBrush(Colors.IndianRed), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox numInput = new NumberBox() { Value = MathF.Round(x, 4), Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            Rectangle rectangleG = new Rectangle() { Fill = new SolidColorBrush(Colors.SeaGreen), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox num2Input = new NumberBox() { Value = MathF.Round(y, 4), Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            Rectangle rectangleB = new Rectangle() { Fill = new SolidColorBrush(Colors.DodgerBlue), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox num3Input = new NumberBox() { Value = MathF.Round(z, 4), Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            return WrapInField(s, rectangleR, numInput, rectangleG, num2Input, rectangleB, num3Input);
        }

        private Grid CreateSlider(string s = "Slider", float i = 0)
        {
            Slider numInput = new Slider() { Value = i, Width = 200, Margin = new Thickness(0, 0, 0, -5.5) };
            TextBlock numPreview = new TextBlock() { Padding = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            return WrapInField(s, numInput, numPreview);
        }

        private Grid CreateBool(string s = "Bool", bool b = false)
        {
            CheckBox check = new CheckBox() { IsChecked = b, Margin = new Thickness(0, 0, 0, -5.5) };

            return WrapInField(s, check);
        }

        private Grid CreateEnum(string s = "ComboBox", params string[] items)
        {
            ComboBox comboBox = new ComboBox() { FontSize = 13.5f, HorizontalAlignment = HorizontalAlignment.Stretch };

            foreach (var item in items)
                comboBox.Items.Add(item);

            comboBox.SelectedIndex = 0;

            return WrapInField(s, comboBox);
        }

        private Grid CreateTextureSlot(string s = "Texture")
        {
            Grid container = new Grid() { Width = 48, Height = 48 };
            Image img = new Image() { Stretch = Stretch.UniformToFill };
            Button button = new Button() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            TextBlock path = new TextBlock() { Text = "None", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            container.Children.Add(img);
            container.Children.Add(button);

            return WrapInField(s, container, path);
        }

        private Grid CreateReferenceSlot(string s = "Reference")
        {
            Button button = new Button() { Content = "..." };
            TextBlock reference = new TextBlock() { Text = "None (type)", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            return WrapInField(s, button, reference);
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

        private Grid WrapInExpander(Grid content, string s = "Expander")
        {
            Grid grid = new Grid();
            Expander expander = new Expander() { Header = s, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };

            expander.Content = content;
            grid.Children.Add(expander);

            return grid;
        }

        private Grid CreateExpander(string s = "ExampleScript", params Grid[] properties)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            StackPanel stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 10 };
            Expander expander = new Expander() { Header = s, ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
            expander.IsExpanded = true;

            foreach (var item in properties)
                stack.Children.Add(item);

            expander.Content = stack;
            grid.Children.Add(expander);

            return grid;
        }

        private Grid CreateExpanderWithToggleButton(string s = "ExampleScript", params Grid[] properties)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            StackPanel stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 10 };
            Expander expander = new Expander() { Header = s, ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
            expander.Header = new ToggleButton() { Content = s, IsChecked = true };

            foreach (var item in properties)
                stack.Children.Add(item);

            expander.Content = stack;
            grid.Children.Add(expander);

            return grid;
        }

        private Grid CreateExpanderWithEditableHeader(string s = "ExampleScript", params Grid[] properties)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            StackPanel stack = new StackPanel() { Orientation = Orientation.Vertical, Spacing = 10 };
            Expander expander = new Expander() { Header = s, ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Left };
            expander.Header = new TextBox() { Text = s, Margin = new Thickness(0) };

            foreach (var item in properties)
                stack.Children.Add(item);

            expander.Content = stack;
            grid.Children.Add(expander);

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

        private async void SelectImageAsync(Image image, TextBlock path)
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

        private async void SelectFileAsync(TextBlock path)
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

        private static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }
    }
}
