using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Windows.Foundation;
using Color = System.Drawing.Color;
using Path = System.IO.Path;
using Texture = Vortice.Direct3D11.Texture2DArrayShaderResourceView;

namespace Editor.Controller;

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
                CreateVec3InputWithRGB(
                    entity.Transform.LocalPosition,
                    (s, e) => entity.Transform.LocalPosition.X = (float)e.NewValue,
                    (s, e) => entity.Transform.LocalPosition.Y = (float)e.NewValue,
                    (s, e) => entity.Transform.LocalPosition.Z = (float)e.NewValue)
                .WrapInField("Position"),
                CreateVec3InputWithRGBAndTransform(
                    entity.Transform.EulerAngles,
                    entity.Transform)
                .WrapInField("Rotation"),
                CreateVec3InputWithRGB(
                    entity.Transform.LocalScale,
                    (s, e) => entity.Transform.LocalScale.X = (float)e.NewValue,
                    (s, e) => entity.Transform.LocalScale.Y = (float)e.NewValue,
                    (s, e) => entity.Transform.LocalScale.Z = (float)e.NewValue)
                .WrapInField("Scale"),
            };

        _stackPanel.Children.Add(
            properties.StackInGrid()
            .WrapInExpanderWithEditableHeaderAndCheckBox(
                entity.Name,
                true,
                (s, r) => entity.Name = (s as TextBox).Text,
                (s, r) => entity.IsEnabled = (s as CheckBox).IsChecked.Value));

        _stackPanel.Children.Add(CreateSeperator());

        _stackPanel.Children.Add(
            transform.StackInGrid()
            .WrapInExpander("Transform"));

        _stackPanel.Children.Add(
            CreateButtonWithAutoSuggesBoxAndComponentCollector(
                "Add Component",
                (s, e) =>
                {
                    entity.AddComponent(Engine.Core.Instance.RuntimeCompiler.ComponentCollector.GetComponent(e.SelectedItem.ToString()));

                    Properties.Clear();
                    Properties.Set(new ModelView.Properties(entity));
                }));

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
                    if ((newFieldGrid = CreateFromFieldInfo(info.GetValue(component), info, nonPublicFieldInfos)) is not null)
                        fieldsCollection.Add(newFieldGrid);

                // Add events to the events collection.
                foreach (var info in eventsInfos)
                    if ((newFieldGrid = CreateFromEventInfo(info, nonPublicEventsInfos)) is not null)
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
        // Deselect any highlighted Entity from the TreeViews.
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
    public Grid CreateButtonWithAutoSuggesBoxAndComponentCollector(string s,
        TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> suggestionChosen)
    {
        Grid grid = new();

        Button button = new() { Content = s, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(10) };

        AutoSuggestBox autoSuggestBox = new() { Width = 200 };
        autoSuggestBox.TextChanged += (s, e) =>
        {
            // Since selecting an item will also change the text,
            // only listen to changes caused by user entering text.
            if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string[] itemSource = Engine.Core.Instance.RuntimeCompiler.ComponentCollector.Components.ConvertAll<string>(Type => Type.Name).ToArray();
                List<string> suitableItems = new();

                foreach (var component in itemSource)
                    if (component.ToLower().Contains(s.Text.ToLower()))
                        suitableItems.Add(component);

                s.ItemsSource = suitableItems;
            }
        };
        autoSuggestBox.SuggestionChosen += suggestionChosen;

        button.Flyout = new Flyout() { Content = autoSuggestBox };

        grid.Children.Add(button);

        return grid;
    }

    public Grid CreateFromFieldInfo(object value, FieldInfo fieldInfo, FieldInfo[] nonPublic)
    {
        // Initialize a new List of Grid type.
        List<Grid> grid = new();

        // Get the type of the current field.
        var type = fieldInfo.FieldType;
        // Get any custom attributes applied to the field.
        var attributes = fieldInfo.GetCustomAttributes(true);


        #region // Process Attributes
        // Return null if the field has a HideAttribute applied.
        if (attributes.OfType<HideAttribute>().Any())
            return null;

        // Return null if the field doesn't have a ShowAttribute and is not a non-public field.
        if (!attributes.OfType<ShowAttribute>().Any())
            foreach (var info in nonPublic)
                if (fieldInfo.Equals(info))
                    return null;

        ToolTip toolTip = new();
        // Create a ToolTip if the field has a ToolTipAttribute and is not a non-public field.
        if (attributes.OfType<ToolTipAttribute>().Any())
            toolTip.Content = (string)attributes.OfType<ToolTipAttribute>().First().ToolTip;
        #endregion

        #region // Process FieldType
        // Check the type of the field and add the appropriate element to the `grid` list.

        // Color
        if (type == typeof(Color))
            grid.Add(
                CreateColorButton(
                    ((Color)value).R,
                    ((Color)value).G,
                    ((Color)value).B,
                    ((Color)value).A));

        // Byte
        else if (type == typeof(byte))
            // If the field has the `SliderAttribute`, add a slider element.
            if (attributes.OfType<SliderAttribute>().Any())
                grid.Add(
                    CreateSlider(
                        (byte)value,
                        (byte)attributes.OfType<SliderAttribute>().First().CustomMin,
                        (byte)attributes.OfType<SliderAttribute>().First().CustomMax)
                    .WrapInGrid());
            // If the field doesn't have the `SliderAttribute`, add a number input element.
            else
                grid.Add(CreateNumberInput((byte)value));

        // Int
        else if (type == typeof(int))
            // If the field has the `SliderAttribute`, add a slider element.
            if (attributes.OfType<SliderAttribute>().Any())
                grid.Add(
                    CreateSlider(
                        (int)value,
                        (int)attributes.OfType<SliderAttribute>().First().CustomMin,
                        (int)attributes.OfType<SliderAttribute>().First().CustomMax)
                    .WrapInGrid());
            // If the field doesn't have the `SliderAttribute`, add a number input element.
            else
                grid.Add(CreateNumberInput((int)value));

        // Float
        else if (type == typeof(float))
            // If the field has the `SliderAttribute`, add a slider element.
            if (attributes.OfType<SliderAttribute>().Any())
                grid.Add(
                    CreateSlider(
                        (float)value,
                        (float)attributes.OfType<SliderAttribute>().First().CustomMin,
                        (float)attributes.OfType<SliderAttribute>().First().CustomMax)
                    .WrapInGrid());
            // If the field doesn't have the `SliderAttribute`, add a number input element.
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
            // Check if value is null.
            if (value is null)
                // Add empty reference slot.
                grid.Add(CreateReferenceSlot("None", type.ToString().FormatString()));
            else
                // Add a reference slot with entity name.
                grid.Add(CreateReferenceSlot(((Entity)value).Name, type.ToString().FormatString()));

        // Component.
        else if (type == typeof(Component))
            // Check if value is null.
            if (value is null)
                // Add empty reference slot.
                grid.Add(CreateReferenceSlot("None", type.ToString().FormatString()));
            else
                // Add a reference slot with component.
                grid.Add(CreateReferenceSlot(((Component)value).ToString().FormatString(), type.ToString().FormatString()));

        // Event.
        else if (type == typeof(EventHandler))
            // Check if value is null.
            if (value is null)
                // Add empty reference slot.
                grid.Add(CreateReferenceSlot("None", type.ToString().FormatString()));
            else
                // Add a reference slot with event.
                grid.Add(CreateReferenceSlot(((EventHandler)value).ToString().SplitLast('.'), type.ToString().FormatString()));

        // Handle default type value.
        else
            // Add empty reference slot.
            grid.Add(CreateReferenceSlot("None", type.ToString().FormatString()));
        #endregion


        // Return the final grid by stacking all the processed attributes, type grid and wrapping the field name.
        return
            (new Grid[] { 
                    // Stack processed attributes in a grid.
                    ProcessAttributes(attributes).StackInGrid(),
                    // Stack field grid and wrap it with field name.
                    grid.ToArray().StackInGrid().WrapInField(fieldInfo.Name)})
            .StackInGrid(0).AddToolTip(toolTip);
    }

    public Grid CreateFromEventInfo(EventInfo eventInfo, EventInfo[] nonPublic)
    {
        // Get any custom attributes applied to the field.
        var attributes = eventInfo.GetCustomAttributes(true);

        // Return null if the field has a HideAttribute applied.
        if (attributes.OfType<HideAttribute>().Any())
            return null;

        // Return null if the field doesn't have a ShowAttribute and is not a non-public field.
        if (!attributes.OfType<ShowAttribute>().Any())
            foreach (var info in nonPublic)
                if (eventInfo.Equals(info))
                    return null;

        // Create the grid that contains the event information and attributes.
        return
            (new Grid[] {
                    // Stack processed attributes in a grid.
                    ProcessAttributes(attributes).StackInGrid(),
                    // Stack event grid and wrap it with field name.
                    CreateEvent(eventInfo.Name, (s, e) => eventInfo.GetRaiseMethod()).WrapInField(eventInfo.Name)})
            .StackInGrid(0);
    }

    public Grid[] ProcessAttributes(object[] attributes)
    {
        // Create a list of grids
        List<Grid> grid = new List<Grid>();

        // Iterate through the attributes
        foreach (var attribute in attributes)
        {
            // HeaderAttribute
            if (attribute.GetType().Equals(typeof(HeaderAttribute)))
                // Create a header with the custom header value and add it to the grid
                grid.Add(CreateHeader((string)((HeaderAttribute)attribute).CustomHeader));

            // SpacerAttribute
            if (attribute.GetType().Equals(typeof(SpacerAttribute)))
                // Create a spacer and add it to the grid
                grid.Add(CreateSpacer());
        }

        // Return the grid as an array
        return grid.ToArray();
    }
}
