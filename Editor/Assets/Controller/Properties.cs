using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System;
using Windows.Foundation;

using Engine.ECS;
using Engine.Editor;
using Engine.Rendering;
using Engine.Runtime;

using static Editor.Controller.Helper;

using Color = System.Drawing.Color;
using Path = System.IO.Path;
using Texture = Vortice.Direct3D11.Texture2DArrayShaderResourceView;

namespace Editor.Controller;

internal sealed partial class Properties
{
    private static StackPanel s_stackPanel = new();

    private static object s_currentlySet;

    public Properties(StackPanel stackPanel, object content)
    {
        s_stackPanel.Children.Clear();
        s_stackPanel = stackPanel;

        if (content is null)
            CreateEmptyMessage();
        else if (content.GetType() == typeof(Entity))
            CreateEntityProperties((Entity)content);
        else if (content.GetType() == typeof(MaterialEntry))
            CreateMaterialProperties((MaterialEntry)content);
        else if (content.GetType() == typeof(string))
            CreateFilePreviewer((string)content);
    }

    public static void Set(object content = null)
    {
        s_currentlySet = content;

        // Clear the children of the PropertiesRoot element in the LayoutControl.
        Main.Instance.LayoutControl.PropertiesRoot.Children.Clear();
        // Set the children of the PropertiesRoot element in the LayoutControl.
        Main.Instance.LayoutControl.PropertiesRoot.Children.Add(new ModelView.Properties(content));
    }

    private void CreateEmptyMessage()
    {
        Grid grid = new() { Margin = new(0, 20, 0, 0) };

        StackPanel stack = new() { HorizontalAlignment = HorizontalAlignment.Center };

        TextBlock textBlock = new() { Text = "Select a file or an entity", Opacity = 0.5f, HorizontalAlignment = HorizontalAlignment.Center };
        TextBlock textBlock2 = new() { Text = "to view its properties.", Opacity = 0.5f, HorizontalAlignment = HorizontalAlignment.Center };

        stack.Children.Add(textBlock);
        stack.Children.Add(textBlock2);

        grid.Children.Add(stack);

        s_stackPanel.Children.Add(grid);
    }

    private void CreateEntityProperties(Entity entity)
    {
        // Add Bindings for the Entity.
        Binding.SetBindings(entity);

        Grid[] properties = new[]
        {
                CreateBool(entity.ID, null, "IsStatic", false).WrapInField("Static"),
                CreateEnum(Enum.GetNames(typeof(Tags))).WrapInField("Tag"),
                CreateEnum(Enum.GetNames(typeof(Layers))).WrapInField("Layer"),
                CreateTextFullWithOpacity(entity.GetDebugInformation()).WrapInField("Debug")
        };

        Grid[] transform = new[]
        {
                CreateVec3InputWithRGB(
                    entity.ID,
                    entity.Transform, "LocalPosition",
                    entity.Transform.LocalPosition)
                .WrapInField("Position"),
                CreateQuaternionInputWithRGBFromEuler(
                    entity.ID,
                    entity.Transform, "_localRotation",
                    entity.Transform.LocalRotation)
                .WrapInField("Rotation"),
                CreateVec3InputWithRGB(
                    entity.ID,
                    entity.Transform, "LocalScale",
                    entity.Transform.LocalScale)
                .WrapInField("Scale"),
            };

        s_stackPanel.Children.Add(
            properties.StackInGrid()
            .WrapInExpanderWithEditableHeaderAndCheckBox(
                entity.ID,
                entity.Name,
                true));

        s_stackPanel.Children.Add(CreateSeperator());

        s_stackPanel.Children.Add(
            transform.StackInGrid()
            .WrapInExpander("Transform"));

        s_stackPanel.Children.Add(
            CreateButtonWithAutoSuggestBoxAndComponentCollector(
                "Add Component",
                (s, e) =>
                {
                    entity.AddComponent(
                        ScriptCompiler.ComponentLibrary
                        .GetComponent(e.SelectedItem.ToString()));

                    Set(entity);
                }));

        // Iterate through all the components of the given entity.
        foreach (var component in entity.GetComponents())
        {
            // Skip the Transform component of the entity.
            if (component != entity.Transform)
            {
                BindingFlags allBindingFlags = BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

                // Get the public, non-public fields and events of the component.
                var nonPublicFieldInfos = component.GetType().GetFields(allBindingFlags);
                var fieldInfos = component.GetType().GetFields(allBindingFlags | BindingFlags.Public);
                var nonPublicEventsInfos = component.GetType().GetEvents(allBindingFlags);
                var eventsInfos = component.GetType().GetEvents(allBindingFlags | BindingFlags.Public);

                // Initialize the collection of fields, events and the final collection.
                Grid newFieldGrid;
                List<Grid> fieldsCollection = new();
                List<Grid> eventsCollection = new();
                List<Grid> finalCollection = new();

                // Add fields to the fields collection.
                foreach (var fieldInfo in fieldInfos)
                    if ((newFieldGrid = CreateFromComponentFieldInfo(component, entity, fieldInfo, nonPublicFieldInfos)) is not null)
                        fieldsCollection.Add(newFieldGrid);

                // Add events to the events collection.
                foreach (var info in eventsInfos)
                    if ((newFieldGrid = CreateFromEventInfo(info, nonPublicEventsInfos)) is not null)
                        eventsCollection.Add(newFieldGrid);

                // Add all the fields and events to the final collection.
                finalCollection.AddRange(fieldsCollection);
                finalCollection.AddRange(eventsCollection);

                // Initialize the content grid and stack panel.
                UIElement tmp;
                Grid content = new Grid();
                s_stackPanel.Children.Add(tmp = finalCollection.ToArray()
                    .StackInGrid()
                    .WrapInExpanderWithToggleButton(
                        ref content,
                        entity.ID,
                        component)
                    .AddContentFlyout(CreateDefaultMenuFlyout(entity, component)));

                // Add an event handler to remove the current component from the stack panel when it's destroyed.
                component.EventOnDestroy += (s, e) => s_stackPanel.Children.Remove(tmp);
            }

            // Add an event handler to update the current stackPanel when a new component is added.
            entity.Components.OnAdd += (s, e) =>
            {
                if (s_currentlySet.Equals(entity))
                    Set(entity);
            };
        }
    }

    private void CreateMaterialProperties(MaterialEntry materialEntry)
    {
        // Add Bindings for the Material.
        Binding.SetBindings(materialEntry);

        s_stackPanel.Children.Add(
            CreateButtonWithAutoSuggestBoxAndShaderCollector(
                materialEntry.ShaderEntry.FileInfo.Name,
                (s, e) =>
                {
                    string newShaderName = e.SelectedItem.ToString();

                    var newShaderEntry = ShaderCompiler.ShaderLibrary
                        .GetShader(newShaderName);

                    if(newShaderEntry is null)
                    {
                        Output.Log($"Getting the Shader from the ShaderName {newShaderName} failed", MessageType.Error);
                        return;
                    }

                    materialEntry.SetShader(newShaderEntry);

                    Set(materialEntry);
                }));

        s_stackPanel.Children.Add(CreateSeperator());

        // Get the fields of the properties constantbuffer.
        var fieldInfos = materialEntry.Material.MaterialBuffer.GetPropertiesConstantBuffer().GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance);

        // Initialize the collection of fields, events, and scripts.
        Grid newFieldGrid;
        List<Grid> fieldsCollection = new();

        // Add fields to the fields collection.
        foreach (var fieldInfo in fieldInfos)
            if ((newFieldGrid = CreateFromMaterialFieldInfo(materialEntry, fieldInfo)) is not null)
                fieldsCollection.Add(newFieldGrid);

        // Initialize the content grid and stack panel.
        UIElement tmp;
        Grid content = new Grid();
        s_stackPanel.Children.Add(tmp = fieldsCollection.ToArray()
            .StackInGrid()
            .WrapInExpander("Properties"));
    }

    private async void CreateFilePreviewer(string path)
    {
        // Deselect any highlighted Entity from the TreeViews.
        Main.Instance.LayoutControl.Hierarchy.HierarchyControl.DeselectTreeViewNodes();

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

        s_stackPanel.Children.Add(properties.StackInGrid().WrapInExpander(Path.GetFileName(path), false));
        s_stackPanel.Children.Add(CreateSeperator());
        s_stackPanel.Children.Add(CreateButton("Open File", (s, e) =>
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
                s_stackPanel.Children.Add(preview.StackInGrid().WrapInExpander("Preview"));
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

internal sealed partial class Properties
{
    public Grid CreateButtonWithAutoSuggestBoxAndComponentCollector(string label,
        TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> suggestionChosen)
    {
        Grid grid = new();

        Button button = new() { Content = label, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(10) };

        AutoSuggestBox autoSuggestBox = new() { Width = 200, IsSuggestionListOpen = true };
        autoSuggestBox.TextChanged += (s, e) =>
        {
            // Since selecting an item will also change the text,
            // only listen to changes caused by user entering text.
            if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string[] itemSource = ScriptCompiler.ComponentLibrary
                    .Components.ConvertAll<string>(Type => Type.Name).ToArray();

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

    public Grid CreateButtonWithAutoSuggestBoxAndShaderCollector(string label,
            TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> suggestionChosen)
    {
        Grid grid = new();

        Button button = new() { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new(10) };

        AutoSuggestBox autoSuggestBox = new() { Width = 200, IsSuggestionListOpen = true };
        autoSuggestBox.TextChanged += (s, e) =>
        {
            // Since selecting an item will also change the text,
            // only listen to changes caused by user entering text.
            if (e.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string[] itemSource = ShaderCompiler.ShaderLibrary
                    .Shaders.ConvertAll<string>(ShaderEntry => ShaderEntry.FileInfo.Name).ToArray();

                List<string> suitableItems = new();

                foreach (var shaders in itemSource)
                    if (shaders.ToLower().Contains(s.Text.ToLower()))
                        suitableItems.Add(shaders);

                s.ItemsSource = suitableItems;
            }
        };
        autoSuggestBox.SuggestionChosen += suggestionChosen;

        button.Flyout = new Flyout() { Content = autoSuggestBox };

        grid.Children.Add(button);

        return grid;
    }

    public Grid CreateFromComponentFieldInfo(object component, Entity entity, FieldInfo fieldInfo, FieldInfo[] nonPublic)
    {
        Grid finalGrid = null;

        // Initialize a new List of Grid type.
        List<Grid> grid = new();

        var value = fieldInfo.GetValue(component);
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

        // Create a ToolTip if the field has a ToolTipAttribute.
        ToolTip toolTip = new();
        if (attributes.OfType<ToolTipAttribute>().Any())
            toolTip.Content = (string)attributes.OfType<ToolTipAttribute>().First().ToolTip;

        // Create a Text with Opacity and return early.
        if (attributes.OfType<ShowOnlyAttribute>().Any())
        {
            grid.Add(CreateTextWithOpacity(entity.ID, component, fieldInfo.Name, value.ToString()));

            finalGrid = ReturnProcessedFieldInfo(grid, attributes, fieldInfo, toolTip);
        }
        #endregion

        if (finalGrid is null)
        {
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
                        CreateSliderInt(
                            entity.ID, component, fieldInfo.Name,
                            (byte)value,
                            (byte)attributes.OfType<SliderAttribute>().First().CustomMin,
                            (byte)attributes.OfType<SliderAttribute>().First().CustomMax));
                // If the field doesn't have the `SliderAttribute`, add a number input element.
                else
                    grid.Add(CreateNumberInputInt(
                        entity.ID, component, fieldInfo.Name,
                        (byte)value,
                        byte.MinValue,
                        byte.MaxValue));

            // Int
            else if (type == typeof(int))
                // If the field has the `SliderAttribute`, add a slider element.
                if (attributes.OfType<SliderAttribute>().Any())
                    grid.Add(
                        CreateSliderInt(
                            entity.ID, component, fieldInfo.Name,
                            (int)value,
                            (int)attributes.OfType<SliderAttribute>().First().CustomMin,
                            (int)attributes.OfType<SliderAttribute>().First().CustomMax));
                // If the field doesn't have the `SliderAttribute`, add a number input element.
                else
                    grid.Add(CreateNumberInputInt(
                        entity.ID, component, fieldInfo.Name,
                        (int)value));

            // Float
            else if (type == typeof(float))
                // If the field has the `SliderAttribute`, add a slider element.
                if (attributes.OfType<SliderAttribute>().Any())
                    grid.Add(
                        CreateSlider(
                            entity.ID, component, fieldInfo.Name,
                            (float)value,
                            (float)attributes.OfType<SliderAttribute>().First().CustomMin,
                            (float)attributes.OfType<SliderAttribute>().First().CustomMax));
                // If the field doesn't have the `SliderAttribute`, add a number input element.
                else
                    grid.Add(CreateNumberInput(
                        entity.ID, component, fieldInfo.Name,
                        (float)value));

            // String
            else if (type == typeof(string))
                grid.Add(CreateTextInput(entity.ID, component, fieldInfo.Name, (string)value));

            // Vector 2
            else if (type == typeof(Vector2))
                grid.Add(CreateVec2Input(entity.ID, component, fieldInfo.Name, (Vector2)value));

            // Vector 3
            else if (type == typeof(Vector3))
                grid.Add(CreateVec3Input(entity.ID, component, fieldInfo.Name, (Vector3)value));

            // Vector 3
            else if (type == typeof(Vector4))
                grid.Add(CreateVec4Input(entity.ID, component, fieldInfo.Name, (Vector4)value));

            // Bool
            else if (type == typeof(bool))
                grid.Add(CreateBool(entity.ID, component, fieldInfo.Name, (bool)value));

            // Enum
            else if (type.IsEnum)
                grid.Add(CreateComboBox(type, entity.ID, component, fieldInfo.Name, value.ToString()));

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
            finalGrid = ReturnProcessedFieldInfo(grid, attributes, fieldInfo, toolTip);
        }

        #region // Final Process Attributes
        if (attributes.OfType<IfAttribute>().Any())
        {
            var attribute = attributes.OfType<IfAttribute>().First();
            var bindEntry = Binding.GetBinding(attribute.FieldName, component, entity.ID);

            bindEntry.Event += (s, e) =>
            {
                var result = Equals(
                     attribute.Value.ToString(),
                     bindEntry?.Value.ToString());

                finalGrid.Visibility = result
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };
        }

        if (attributes.OfType<IfNotAttribute>().Any())
        {
            var attribute = attributes.OfType<IfNotAttribute>().First();
            var bindEntry = Binding.GetBinding(attribute.FieldName, component, entity.ID);

            bindEntry.Event += (s, e) =>
            {
                var result = Equals(
                     attribute.Value.ToString(),
                     bindEntry?.Value.ToString());

                finalGrid.Visibility = result
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            };
        }
        #endregion

        return finalGrid;
    }

    public Grid CreateFromMaterialFieldInfo(MaterialEntry materialEntry, FieldInfo fieldInfo)
    {
        Grid finalGrid = null;

        // Initialize a new List of Grid type.
        List<Grid> grid = new();

        var propertiesConstantBuffer = materialEntry.Material.MaterialBuffer.GetPropertiesConstantBuffer();
        var value = fieldInfo.GetValue(propertiesConstantBuffer);
        // Get the type of the current field.
        var type = fieldInfo.FieldType;
        // Get any custom attributes applied to the field.
        var attributes = fieldInfo.GetCustomAttributes(true);

        if (finalGrid is null)
        {
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

            // Int
            else if (type == typeof(int))
                // If the field has the `SliderAttribute`, add a slider element.
                if (attributes.OfType<SliderAttribute>().Any())
                    grid.Add(
                        CreateSliderInt(null,
                            propertiesConstantBuffer, fieldInfo.Name,
                            (int)value,
                            (int)attributes.OfType<SliderAttribute>().First().CustomMin,
                            (int)attributes.OfType<SliderAttribute>().First().CustomMax));
                // If the field doesn't have the `SliderAttribute`, add a number input element.
                else
                    grid.Add(CreateNumberInputInt(null,
                        propertiesConstantBuffer, fieldInfo.Name,
                        (int)value));

            // Float
            else if (type == typeof(float))
                // If the field has the `SliderAttribute`, add a slider element.
                if (attributes.OfType<SliderAttribute>().Any())
                    grid.Add(
                        CreateSlider(null,
                            propertiesConstantBuffer, fieldInfo.Name,
                            (float)value,
                            (float)attributes.OfType<SliderAttribute>().First().CustomMin,
                            (float)attributes.OfType<SliderAttribute>().First().CustomMax));
                // If the field doesn't have the `SliderAttribute`, add a number input element.
                else
                    grid.Add(CreateNumberInput(null,
                        propertiesConstantBuffer, fieldInfo.Name,
                        (float)value));

            // Vector 2
            else if (type == typeof(Vector2))
                grid.Add(CreateVec2Input(null,
                    propertiesConstantBuffer, fieldInfo.Name, 
                    (Vector2)value));

            // Vector 3
            else if (type == typeof(Vector3))
                grid.Add(CreateVec3Input(null,
                    propertiesConstantBuffer, fieldInfo.Name,
                    (Vector3)value));

            // Vector 4
            else if (type == typeof(Vector4))
                grid.Add(CreateVec4Input(null,
                    propertiesConstantBuffer, fieldInfo.Name,
                    (Vector4)value));

            // Bool
            else if (type == typeof(bool))
                grid.Add(CreateBool(null, 
                    propertiesConstantBuffer, fieldInfo.Name,
                    (bool)value));
            #endregion

            // Return the final grid by stacking all the processed attributes, type grid and wrapping the field name.
            finalGrid = ReturnProcessedFieldInfo(grid, attributes, fieldInfo, null);
        }

        Binding.GetMaterialBinding(fieldInfo.Name, propertiesConstantBuffer)?.SetEvent((s, e) =>
        {
            materialEntry.Material.MaterialBuffer.SafeToSerializableProperties();

            Engine.Helper.Serialization.SaveXml(materialEntry.Material.MaterialBuffer, materialEntry.FileInfo.FullName);

            materialEntry.Material.MaterialBuffer.UpdatePropertiesConstantBuffer();
        });

        return finalGrid;
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

        ToolTip toolTip = new();
        // Create a ToolTip if the field has a ToolTipAttribute.
        if (attributes.OfType<ToolTipAttribute>().Any())
            toolTip.Content = (string)attributes.OfType<ToolTipAttribute>().First().ToolTip;

        // Create the grid that contains the event information and attributes.
        return
            (new Grid[] {
                    // Stack processed attributes in a grid.
                    ProcessAttributes(attributes).StackInGrid(),
                    // Stack event grid and wrap it with field name.
                    CreateEvent(eventInfo.Name, (s, e) => eventInfo.GetRaiseMethod()).WrapInField(eventInfo.Name)})
            .StackInGrid(0).AddToolTip(toolTip);
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
            if (attribute.GetType().Equals(typeof(SpaceAttribute)))
                // Create a spacer and add it to the grid
                grid.Add(CreateSpacer());
        }

        // Return the grid as an array
        return grid.ToArray();
    }

    public Grid ReturnProcessedFieldInfo(List<Grid> grid, object[] attributes, FieldInfo fieldInfo, ToolTip toolTip) =>
        new Grid[] {
            // Stack processed attributes in a grid.
            ProcessAttributes(attributes).StackInGrid(),
            // Stack field grid and wrap it with field name.
            grid.ToArray().StackInGrid().WrapInField(fieldInfo.Name)}
        .StackInGrid(0).AddToolTip(toolTip);
}
