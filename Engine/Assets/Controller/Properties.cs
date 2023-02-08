using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Linq;
using System.Reflection;
using System;
using Vortice.Mathematics;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Storage;
using Engine.ECS;
using Engine.Editor;
using Engine.Utilities;
using Image = Microsoft.UI.Xaml.Controls.Image;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using Texture = Vortice.Direct3D11.Texture2DArrayShaderResourceView;

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

        public static void Clear() =>
            // Clear the children of the PropertiesRoot element in the LayoutControl.
            Main.Instance.LayoutControl.PropertiesRoot.Children.Clear();

        public static void Set(ModelView.Properties properties) =>
            // Set the children of the PropertiesRoot element in the LayoutControl.
            Main.Instance.LayoutControl.PropertiesRoot.Children.Add(properties);

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
                CreateBool(false, (s, r) => entity.IsStatic = (s as CheckBox).IsChecked.Value).WrapInField("Is Static"),
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
                    entity.Transform.Rotation.ToEuler().ToDegrees(),
                    (s, e) =>  entity.Transform.EulerAngles.X = (float)e.NewValue,
                    (s, e) =>  entity.Transform.EulerAngles.Y = (float)e.NewValue,
                    (s, e) =>  entity.Transform.EulerAngles.Z = (float) e.NewValue).WrapInField("Rotation"),
                CreateVec3InputTransform(
                    entity.Transform.Scale,
                    (s, e) => entity.Transform.Scale.X = (float)e.NewValue,
                    (s, e) => entity.Transform.Scale.Y = (float)e.NewValue,
                    (s, e) => entity.Transform.Scale.Z = (float)e.NewValue).WrapInField("Scale"),
            };

            _stackPanel.Children.Add(properties.StackInGrid().WrapInExpanderWithEditableHeaderAndCheckBox(entity.Name, true, (s, r) => entity.Name = (s as TextBox).Text, (s, r) => entity.IsEnabled = (s as CheckBox).IsChecked.Value));
            _stackPanel.Children.Add(CreateSeperator());
            _stackPanel.Children.Add(transform.StackInGrid().WrapInExpander("Transform"));
            _stackPanel.Children.Add(CreateButton("Add Component", null));

            // Iterate through all the components of the given entity.
            foreach (var component in entity.GetComponents())
            {
                // Skip the Transform component of the entity.
                if (component != entity.Transform)
                {
                    // Get the non-public fields and events of the component.
                    var nonPublicFieldInfos = component.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    var fieldInfos = component.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                    var nonPublicEventsInfos = component.GetType().GetEvents(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    var eventsInfos = component.GetType().GetEvents(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);

                    // Initialize the collection of fields, events, and scripts.
                    Grid newFieldGrid;
                    List<Grid> fieldsCollection = new();
                    List<Grid> eventsCollection = new();
                    List<Grid> scriptsCollection = new();

                    // Add fields to the fields collection.
                    foreach (var info in fieldInfos)
                        if ((newFieldGrid = CreateFromFieldInfo(info.GetValue(component), info, nonPublicFieldInfos)) != null)
                            fieldsCollection.Add(newFieldGrid);

                    // Add events to the events collection.
                    foreach (var info in eventsInfos)
                        if ((newFieldGrid = CreateFromEventInfo(info, nonPublicEventsInfos)) != null)
                            eventsCollection.Add(newFieldGrid);

                    // Add all the fields and events to the scripts collection.
                    scriptsCollection.AddRange(fieldsCollection);
                    scriptsCollection.AddRange(eventsCollection);

                    // Initialize the content grid and stack panel.
                    UIElement tmp;
                    Grid content = new Grid();
                    _stackPanel.Children.Add(tmp = scriptsCollection.ToArray()
                        .StackInGrid().WrapInExpanderWithToggleButton(ref content, component.ToString().FormatString(), component, "IsEnabled", null)
                        .AddContentFlyout(CreateDefaultMenuFlyout(entity, component)));

                    // Add an event handler to remove the current component from the stack panel when it's destroyed.
                    component._eventOnDestroy += (s, e) => _stackPanel.Children.Remove(tmp);
                }
            }
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
                // Check if the file exists at the specified path and start the process to open it.
                if (File.Exists(path))
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }));


            // Check if the file at the given path exists.
            if (File.Exists(path))
                // When the file extension is in a readable format, continue.
                if (fileInfo.Extension == ".cs"
                    || fileInfo.Extension == ".txt"
                    || fileInfo.Extension == ".usd"
                    || fileInfo.Extension == ".mat"
                    || fileInfo.Extension == ".hlsl")
                {
                    // Read all the lines in the file asynchronously and store them in an array of strings.
                    string[] lines = await File.ReadAllLinesAsync(path);
                    // Join all the lines in the array with a line break to create a single string.
                    string joinedLines = string.Join("\n", lines);

                    // Create a Grid that contains a text element with the joined lines and wrap it in an array.
                    Grid[] preview = new[] { CreateTextFullWithOpacity(joinedLines).WrapInGrid() };

                    // Add the preview to the stack panel, wrapped in an expander with a label "Preview".
                    _stackPanel.Children.Add(preview.StackInGrid().WrapInExpander("Preview"));
                }
        }

        private MenuFlyout CreateDefaultMenuFlyout(Entity entity, Component component)
        {
            // Create an array of MenuFlyoutItems.
            MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
            };

            // Add a click event to the first item in the items array.
            // The event will remove the component from the entity when clicked.
            items[0].Click += (s, e) => entity.RemoveComponent(component);

            // Create a new instance of MenuFlyout.
            MenuFlyout menuFlyout = new();
            // Loop through each item in the items array and add it to the menuFlyout's Items collection.
            foreach (var item in items)
                menuFlyout.Items.Add(item);

            // Return the created menuFlyout.
            return menuFlyout;
        }
    }

    internal partial class Properties : Controller.Helper
    {
        public Grid CreateFromFieldInfo(object value, FieldInfo fieldInfo, FieldInfo[] nonPublic)
        {
            // Initialize a new List of Grid type.
            List<Grid> grid = new();

            // Get the type of the current field.
            var type = fieldInfo.FieldType;
            // Get any custom attributes applied to the field.
            var attributes = fieldInfo.GetCustomAttributes(true);

            // Return null if the field has a HideAttribute applied.
            if (attributes.OfType<HideAttribute>().Any())
                return null;

            // Return null if the field doesn't have a ShowAttribute and is not a non-public field.
            if (!attributes.OfType<ShowAttribute>().Any())
                foreach (var info in nonPublic)
                    if (fieldInfo.Equals(info))
                        return null;

            //#region Get Field Type and Process Value
            //// Color.
            //if (type == typeof(Color))
            //    // Create a color button for the field value.
            //    grid.Add(CreateColorButton(((Color)value).R, ((Color)value).G, ((Color)value).B, ((Color)value).A));

            //// Int.
            //else if (type == typeof(int))
            //    // Check if the field has a SliderAttribute applied.
            //    if (attributes.OfType<SliderAttribute>().Any())
            //    // Create a slider for the field value with custom minimum and maximum values.
            //    { }



            #region // GetFieldType and process Value
            // Color
            if (type == typeof(Color))
                grid.Add(CreateColorButton(((Color)value).R, ((Color)value).G, ((Color)value).B, ((Color)value).A));

            // Int
            else if (type == typeof(int))
                if (attributes.OfType<SliderAttribute>().Any())
                    grid.Add(CreateSlider((int)value, (int)attributes.OfType<SliderAttribute>().First().CustomMin, (int)attributes.OfType<SliderAttribute>().First().CustomMax).WrapInGrid());
                else
                    grid.Add(CreateNumberInput((int)value));

            // Float
            else if (type == typeof(float))
                if (attributes.OfType<SliderAttribute>().Any())
                    grid.Add(CreateSlider((float)value, (float)attributes.OfType<SliderAttribute>().First().CustomMin, (float)attributes.OfType<SliderAttribute>().First().CustomMax).WrapInGrid());
                else
                    grid.Add(CreateNumberInput((float)value));

            // String
            else if (type == typeof(string))
                grid.Add(CreateTextInput((string)value));

            // Vector 2
            else if (type == typeof(Vector2))
                grid.Add(CreateVec2Input((Vector2)value));

            // Vector 3
            else if (type == typeof(Vector3))
                grid.Add(CreateVec3Input((Vector3)value));

            // Bool
            else if (type == typeof(bool))
                grid.Add(CreateBool((bool)value).WrapInGrid());

            // Material
            else if (type == typeof(Material))
                grid.Add(CreateTextureSlot("None", "Material"));

            // Texture
            else if (type == typeof(Texture))
                grid.Add(CreateTextureSlot("None", "Texture"));

            // Entity
            else if (type == typeof(Entity))
                if (value is null)
                    grid.Add(CreateReferenceSlot("None", type.ToString().FormatString()));
                else
                    grid.Add(CreateReferenceSlot(((Entity)value).Name, type.ToString().FormatString()));

            // Component
            else if (type == typeof(Component))
                if (value is null)
                    grid.Add(CreateReferenceSlot("None", type.ToString().FormatString()));
                else
                    grid.Add(CreateReferenceSlot(((Component)value).ToString().FormatString(), type.ToString().FormatString()));

            // Event
            else if (type == typeof(EventHandler))
                if (value is null)
                    grid.Add(CreateReferenceSlot("None", type.ToString().FormatString()));
                else
                    grid.Add(CreateReferenceSlot(((EventHandler)value).ToString().SplitLast('.'), type.ToString().FormatString()));

            // Default
            else
                grid.Add(CreateReferenceSlot("None", type.ToString().FormatString()));
            #endregion

            return (new Grid[]
            {
                    ProcessAttributes(attributes).StackInGrid(),
                    grid.ToArray().StackInGrid().WrapInField(fieldInfo.Name)
            }).StackInGrid(0);
        }

        public Grid CreateFromEventInfo(EventInfo eventInfo, EventInfo[] nonPublic)
        {
            var attributes = eventInfo.GetCustomAttributes(true);

            if (attributes.OfType<HideAttribute>().Any())
                return null;

            if (!attributes.OfType<ShowAttribute>().Any())
                foreach (var info in nonPublic)
                    if (eventInfo.Equals(info))
                        return null;

            return (new Grid[]
            {
                ProcessAttributes(attributes).StackInGrid(),
                CreateEvent(eventInfo.Name, (s, e) => eventInfo.GetRaiseMethod()).WrapInField(eventInfo.Name)
            }).StackInGrid(0);
        }

        public Grid[] ProcessAttributes(object[] attributes)
        {
            List<Grid> grid = new();

            foreach (var attribute in attributes)
            {
                if (attribute.GetType().Equals(typeof(HeaderAttribute)))
                    grid.Add(CreateHeader((string)((HeaderAttribute)attribute).CustomHeader));

                if (attribute.GetType().Equals(typeof(SpacerAttribute)))
                    grid.Add(CreateSpacer());
            }

            return grid.ToArray();
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
