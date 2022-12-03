using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System.Diagnostics;
using System.IO;
using System;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Storage;
using Editor.UserControls;
using Engine.Utilities;
using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using ExpandDirection = Microsoft.UI.Xaml.Controls.ExpandDirection;
using Expander = Microsoft.UI.Xaml.Controls.Expander;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using Path = System.IO.Path;
using Vortice.Mathematics;

namespace Editor.Controller
{
    internal partial class PropertiesController
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

        private void CreateEntityProperties(Entity entity)
        {
            var properties = new Grid[]
            {
                CreateBool(true).WrapInField("Is Acitve"),
                CreateBool().WrapInField("Is Static"),
                CreateEnum("Untagged", "MainCamera", "Respawn", "Player", "Finish", "GameController").WrapInField("Tag"),
                CreateEnum("Default", "Transparent FX", "Ignore Raycast", "Water", "UI").WrapInField("Layer")
            };

            var transform = new Grid[]
            {
                CreateVec3InputTransform(
                    entity.Transform.Position,
                    (s, e) => entity.Transform.Position.X = (float)e.NewValue,
                    (s, e) => entity.Transform.Position.Y = (float)e.NewValue,
                    (s, e) => entity.Transform.Position.Z = (float)e.NewValue).WrapInField("Position"),
                CreateVec3InputTransform(
                    entity.Transform.Rotation.ToEuler(),
                    (s, e) => entity.Transform.Rotation.X = (float)e.NewValue,
                    (s, e) => entity.Transform.Rotation.Y = (float)e.NewValue,
                    (s, e) => entity.Transform.Rotation.Z = (float)e.NewValue).WrapInField("Rotation"),
                CreateVec3InputTransform(
                    entity.Transform.Scale,
                    (s, e) => entity.Transform.Scale.X = (float)e.NewValue,
                    (s, e) => entity.Transform.Scale.Y = (float)e.NewValue,
                    (s, e) => entity.Transform.Scale.Z = (float)e.NewValue).WrapInField("Scale"),
            };

            var collection = new Grid[]
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
            _stackPanel.Children.Add(collection.StackInGrid().WrapInExpanderWithToggleButton("Example"));
            _stackPanel.Children.Add(CreateSpacer().WrapInExpanderWithToggleButton("Another"));
        }

        private async void CreateFilePreviewer(string path)
        {
            MainController.Instance.LayoutControl.Hierarchy._hierarchyControl.DeselectTreeViewNodes();

            FileInfo fileInfo = new FileInfo(path);

            var properties = new Grid[]
            {
                CreateText(Path.GetFileNameWithoutExtension(path)).WrapInField("File name:"),
                CreateText(Path.GetExtension(path)).WrapInField("File type:"),
                CreateText(SizeSuffix(fileInfo.Length)).WrapInField("File size:"),
                CreateSpacer(),
                CreateTextEqual(fileInfo.CreationTime.ToShortDateString() + " " + fileInfo.CreationTime.ToShortTimeString()).WrapInFieldEqual("Creation time:"),
                CreateTextEqual(fileInfo.LastAccessTime.ToShortDateString() + " " + fileInfo.LastAccessTime.ToShortTimeString()).WrapInFieldEqual("Last access time:"),
                CreateTextEqual(fileInfo.LastWriteTime.ToShortDateString() + " " + fileInfo.LastWriteTime.ToShortTimeString()).WrapInFieldEqual("Last update time:")
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

                    Grid[] preview = new Grid[] { CreateTextFullWithOpacity(joinedLines).WrapInGrid() };

                    _stackPanel.Children.Add(preview.StackInGrid().WrapInExpander("Preview"));
                }
        }
    }

    internal partial class PropertiesController : HelperController
    {
        private Grid CreateTextureSlot()
        {
            Grid container = new Grid() { Width = 48, Height = 48 };
            Image img = new Image() { Stretch = Stretch.UniformToFill };
            Button button = new Button() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            TextBlock path = new TextBlock() { Text = "None", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            container.Children.Add(img);
            container.Children.Add(button);

            return StackInGrid(container, path);
        }

        private Grid CreateReferenceSlot()
        {
            Button button = new Button() { Content = "..." };
            TextBlock reference = new TextBlock() { Text = "None (type)", Margin = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            return StackInGrid(button, reference);
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

    }
}
