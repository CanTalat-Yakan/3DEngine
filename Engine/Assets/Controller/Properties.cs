using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System;
using Vortice.Mathematics;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Storage;
using Engine.ECS;
using Path = System.IO.Path;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace Editor.Controller
{
    internal partial class Properties
    {
        private StackPanel _stackPanel;

        public Properties(StackPanel stackPanel, object content)
        {
            _stackPanel = stackPanel;

            if (content is null)
                CreateEmptyMessage();
            else if (content.GetType() == typeof(Entity))
                CreateEntityProperties((Entity)content);
            else if (content.GetType() == typeof(string))
                CreateFilePreviewer((string)content);
        }

        public static void Clear() => Main.Instance.LayoutControl.PropertiesRoot.Children.Clear();

        public static void Set(ModelView.Properties properties) => Main.Instance.LayoutControl.PropertiesRoot.Children.Add(properties);

        private void CreateEmptyMessage()
        {
            Grid grid = new() { Margin = new(0, 20, 0, 0) };

            StackPanel stack = new() { HorizontalAlignment = HorizontalAlignment.Center };

            TextBlock textBlock = new() { Text = "Select a file or an entity", Opacity = 0.5f, HorizontalAlignment = HorizontalAlignment.Center };
            TextBlock textBlock2 = new() { Text = "to view its properties.", Opacity = 0.5f, HorizontalAlignment = HorizontalAlignment.Center };

            stack.Children.Add(textBlock);
            stack.Children.Add(textBlock2);

            grid.Children.Add(stack);

            _stackPanel.Children.Add(grid);
        }

        private void CreateEntityProperties(Entity entity)
        {
            Grid[] properties = new[]
            {
                CreateBool(true).WrapInField("Is Acitve"),
                CreateBool().WrapInField("Is Static"),
                CreateEnum(Enum.GetNames(typeof(ETags))).WrapInField("Tag"),
                CreateEnum(Enum.GetNames(typeof(ELayers))).WrapInField("Layer")
            };

            Grid[] transform = new[]
            {
                CreateVec3InputTransform(
                    entity.Transform.Position,
                    (s, e) => entity.Transform.Position.X = (float)e.NewValue,
                    (s, e) => entity.Transform.Position.Y = (float)e.NewValue,
                    (s, e) => entity.Transform.Position.Z = (float)e.NewValue).WrapInField("Position"),
                CreateVec3InputTransform(
                    entity.Transform.Rotation.ToEuler(),
                    (s, e) =>  entity.Transform.EulerAngles.X = (float)e.NewValue,
                    (s, e) =>  entity.Transform.EulerAngles.Y = (float)e.NewValue,
                    (s, e) =>  entity.Transform.EulerAngles.Z = (float) e.NewValue).WrapInField("Rotation"),
                    //(s, e) =>  entity.Transform.Rotation = Quaternion.Add(entity.Transform.Rotation, Quaternion.CreateFromAxisAngle(Vector3.UnitX, (float)e.NewValue)),
                    //(s, e) =>  entity.Transform.Rotation = Quaternion.Add(entity.Transform.Rotation, Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)e.NewValue)),
                    //(s, e) =>  entity.Transform.Rotation = Quaternion.Add(entity.Transform.Rotation, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float)e.NewValue))).WrapInField("Rotation"),
                CreateVec3InputTransform(
                    entity.Transform.Scale,
                    (s, e) => entity.Transform.Scale.X = (float)e.NewValue,
                    (s, e) => entity.Transform.Scale.Y = (float)e.NewValue,
                    (s, e) => entity.Transform.Scale.Z = (float)e.NewValue).WrapInField("Scale"),
            };

            Grid[] collection = new[]
            {
                CreateColorButton().WrapInField("Color"),
                CreateNumberInput().WrapInField("Float"),
                CreateTextInput().WrapInField("String"),
                CreateVec2Input().WrapInField("Vector 2"),
                CreateVec3Input().WrapInField("Vector 3"),
                CreateSlider().WrapInField("Slider"),
                CreateBool().WrapInField("Bool"),
                CreateTextureSlot().WrapInField("Texture"),
                CreateReferenceSlot().WrapInField("Reference"),
                CreateHeader(),
                CreateEvent().WrapInField("Event").WrapInExpander("Expander")
            };

            _stackPanel.Children.Add(properties.StackInGrid().WrapInExpanderWithEditableHeader(entity.Name));
            _stackPanel.Children.Add(CreateSeperator());
            _stackPanel.Children.Add(transform.StackInGrid().WrapInExpander("Transform"));
            _stackPanel.Children.Add(CreateButton("Add Component", null));

            foreach (var component in entity.GetComponents())
                if (component != entity.Transform)
                {
                    var fieldInfos = component.GetType().GetFields(
                        //BindingFlags.NonPublic |
                        BindingFlags.Static |
                        BindingFlags.Public |
                        BindingFlags.Instance);

                    List<Grid> propertiesCollection = new();
                    foreach (var field in fieldInfos)
                        propertiesCollection.Add(CreateNumberInput().WrapInFieldEqual(field.Name));

                    _stackPanel.Children.Add(propertiesCollection.ToArray().StackInGrid().WrapInExpanderWithToggleButton(component.ToString().Split('.').Last()));
                }

            _stackPanel.Children.Add(collection.StackInGrid().WrapInExpanderWithToggleButton("Expander"));
        }

        private async void CreateFilePreviewer(string path)
        {
            Main.Instance.LayoutControl.Hierarchy._hierarchyControl.DeselectTreeViewNodes();

            FileInfo fileInfo = new(path);

            Grid[] properties = new[]
            {
                CreateText(Path.GetFileNameWithoutExtension(path)).WrapInFieldEqual("File name:"),
                CreateText(Path.GetExtension(path)).WrapInFieldEqual("File type:"),
                CreateText(SizeSuffix(fileInfo.Length)).WrapInFieldEqual("File size:"),
                CreateSpacer(),
                CreateTextEqual(fileInfo.CreationTime.ToShortDateString() + "⠀" + fileInfo.CreationTime.ToShortTimeString()).WrapInFieldEqual("Creation time:"),
                CreateTextEqual(fileInfo.LastAccessTime.ToShortDateString() + "⠀" + fileInfo.LastAccessTime.ToShortTimeString()).WrapInFieldEqual("Last access time:"),
                CreateTextEqual(fileInfo.LastWriteTime.ToShortDateString() + "⠀" + fileInfo.LastWriteTime.ToShortTimeString()).WrapInFieldEqual("Last update time:")
            };

            _stackPanel.Children.Add(properties.StackInGrid().WrapInExpander(Path.GetFileName(path)));
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

                    Grid[] preview = new[] { CreateTextFullWithOpacity(joinedLines).WrapInGrid() };

                    _stackPanel.Children.Add(preview.StackInGrid().WrapInExpander("Preview"));
                }
        }
    }

    internal partial class Properties : Controller.Helper
    {
        private Grid CreateTextureSlot()
        {
            Grid container = new() { Width = 48, Height = 48 };
            Image img = new() { Stretch = Stretch.UniformToFill };
            Button button = new() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            TextBlock path = new() { Text = "None", Margin = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            container.Children.Add(img);
            container.Children.Add(button);

            return StackInGrid(container, path);
        }

        private Grid CreateReferenceSlot()
        {
            Button button = new() { Content = "..." };
            TextBlock reference = new() { Text = "None (type)", Margin = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            return StackInGrid(button, reference);
        }

        private async void SelectImageAsync(Image image, TextBlock path)
        {
            FileOpenPicker picker = new()
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
                    BitmapImage bitmapImage = new() { DecodePixelHeight = 48, DecodePixelWidth = 48 };
                    await bitmapImage.SetSourceAsync(fileStream);
                    image.Source = bitmapImage;
                }

                path.Text = file.Name;
            }
        }

        private async void SelectFileAsync(TextBlock path)
        {
            FileOpenPicker picker = new()
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
                path.Text = file.Name;
        }

    }
}
