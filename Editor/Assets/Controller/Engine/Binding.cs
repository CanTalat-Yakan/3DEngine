using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using Windows.Foundation;

using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

using Engine.ECS;
using Engine.Essentials;
using Engine.Runtime;
using Engine.Editor;

namespace Editor.Controller;

internal sealed class BindEntry(object source, string sourcePath) : IDisposable
{
    public object Value { get; set; }

    public object Source = source;
    public string SourceValuePath = sourcePath;

    public object Target;
    public string TargetValuePath;
    public string TargetEventPath;

    public event Action Event;

    public void Set(object target, string targetValuePath, string targetEventPath = null)
    {
        Target = target;
        TargetValuePath = targetValuePath;
        TargetEventPath = targetEventPath;

        if (!string.IsNullOrEmpty(TargetEventPath))
            Binding.SetPathEvent(this, Binding.AllBindingFlags);
    }

    public void SetEvent(Action e) =>
        Event += e;

    public void Invoke() =>
        Event?.Invoke();

    public void Dispose() =>
        Event = null;
}

internal sealed partial class Binding
{
    // Key = Field + Component.GetType().FullName
    public static Dictionary<string, BindEntry> RendererBindings = new();
    // Key = Field + Scene.ID
    public static Dictionary<string, BindEntry> SceneBindings = new();
    // Key = Field + Entity.ID | Key = Field + Component.GetType().FullName + Entity.ID
    public static Dictionary<string, BindEntry> EntityBindings = new();
    // Key = Field + materialPropertiesConstantBuffer.GetType().FullName
    public static Dictionary<string, BindEntry> MaterialBindings = new();

    public static BindingFlags AllBindingFlags =
          BindingFlags.NonPublic
        | BindingFlags.Public
        | BindingFlags.Static
        | BindingFlags.Instance;

    public static void Update()
    {
        UpdateRendererBindings();
        UpdateSceneBindings();
        UpdateEntityBindings();
        UpdateMaterialBindings();
    }

    public static void Remove(Guid? guid)
    {
        if (SceneBindings is not null && guid is not null)
            foreach (var bind in SceneBindings.ToArray())
                if (bind.Key.Contains(guid.ToString()))
                    SceneBindings.Remove(bind.Key);
    }

    public static void Dispose()
    {
        RendererBindings?.Clear();
        SceneBindings?.Clear();
        EntityBindings?.Clear();
    }

    public static void ClearAndDispose(Dictionary<string, BindEntry> dictionary)
    {
        foreach (var bindEntry in dictionary.Values)
            bindEntry.Dispose();

        dictionary.Clear();
    }
}

internal sealed partial class Binding
{
    public static void SetRendererBindings()
    {
        ClearAndDispose(RendererBindings);
        RendererBindings.Add(
            "FieldOfView" + ViewportController.Camera?.GetType().FullName,
            new(ViewportController.Camera, "FieldOfView"));
        RendererBindings.Add(
            "CameraProjection" + Engine.Kernel.Instance.Config,
            new(Engine.Kernel.Instance.Config, "CameraProjection"));
        RendererBindings.Add(
            "RenderMode" + Engine.Kernel.Instance.Config,
            new(Engine.Kernel.Instance.Config, "RenderMode"));
    }

    public static void SetSceneBindings(EntityManager scene)
    {
        if (scene is null)
            return;
        ClearAndDispose(SceneBindings);
        SceneBindings.Add("Scene@" + scene.GUID, new(scene, "Scene@"));
        SceneBindings.Add("Name" + scene.GUID, new(scene, "Name"));
        SceneBindings.Add("IsEnabled" + scene.GUID, new(scene, "IsEnabled"));
    }

    public static void SetEntityBindings(Entity entity)
    {
        if (entity is null)
            return;

        ClearAndDispose(EntityBindings);
        EntityBindings.Add("Entity@" + entity.GUID, new(entity, "Entity@"));
        EntityBindings.Add("Name" + entity.GUID, new(entity, "Name"));
        EntityBindings.Add("IsStatic" + entity.GUID, new(entity, "IsStatic"));
        EntityBindings.Add("IsEnabled" + entity.GUID, new(entity, "IsEnabled"));

        foreach (var component in entity.GetComponents())
            foreach (var fieldInfo in component.GetType().GetFields(AllBindingFlags))
                if (!fieldInfo.GetCustomAttributes().OfType<HideAttribute>().Any())
                    EntityBindings.TryAdd(fieldInfo.Name + component.GetType().FullName + entity.GUID, new(component, fieldInfo.Name));
    }

    public static void SetMaterialBindings(MaterialEntry materialEntry)
    {
        if (materialEntry is null)
            return;

        ClearAndDispose(MaterialBindings);
        var PropertiesConstantBuffer = Engine.Utilities.Assets.SerializableConstantBuffers[materialEntry.ShaderName].GetConstantBufferObject();
        foreach (var field in PropertiesConstantBuffer.GetType().GetFields(AllBindingFlags))
            MaterialBindings.Add(
                field.Name + PropertiesConstantBuffer.GetType().FullName,
                new(PropertiesConstantBuffer, field.Name));
    }
}

internal sealed partial class Binding
{
    /// <summary>
    /// [Renderer Key = Field.Name + Component.GetType().FullName]   
    /// [Scene Key = Field.Name + Scene.ID]  
    /// [Entity Key = Field.Name + Entity.ID]    
    /// [Component Key = Field.Name + Component.GetType().FullName + Entity.ID]  
    /// [Material Key = Field.Name + materialPropertiesConstantBuffer.GetType().FullName]
    /// </summary>
    public static BindEntry Get(string key, Dictionary<string, BindEntry> dictionary = null)
    {
        if (dictionary is not null)
            if (dictionary.Keys.Contains(key))
                return dictionary[key];

        if (EntityBindings.Keys.Contains(key))
            return EntityBindings[key];
        else if (RendererBindings.Keys.Contains(key))
            return RendererBindings[key];
        else if (MaterialBindings.Keys.Contains(key))
            return MaterialBindings[key];

        return null;
    }

    public static BindEntry GetBinding(string fieldName, object source, object id)
    {
        if (source is null)
            return null;

        var bindEntry = id is not null
            ? GetEntityBinding(fieldName, source, id)
            : GetRendererBinding(fieldName, source);

        if (bindEntry is null)
            bindEntry = GetMaterialBinding(fieldName, source);

        return bindEntry;
    }

    public static BindEntry GetRendererBinding(string fieldName, object component) =>
        Get(fieldName + component?.GetType().FullName, RendererBindings);

    public static BindEntry GetSceneBinding(string fieldName, object sceneID) =>
        Get(fieldName + sceneID, SceneBindings);

    public static BindEntry GetEntityBinding(string fieldName, object component, object entityID) =>
        Get(fieldName + component?.GetType().FullName + entityID, EntityBindings);

    public static BindEntry GetEntityBinding(string fieldName, object entityID) =>
        Get(fieldName + entityID, EntityBindings);

    public static BindEntry GetMaterialBinding(string fieldName, object materialPropertiesConstantBuffer) =>
        Get(fieldName + materialPropertiesConstantBuffer.GetType().FullName, MaterialBindings);
}

internal sealed partial class Binding
{
    private static void UpdateRendererBindings()
    {
        if (RendererBindings.Count == 0)
            return;

        foreach (var source in RendererBindings.Select(kv => kv.Value.Source).ToArray())
            if (source is not null)
                UpdateBinding(source, source.GetType().FullName, RendererBindings);
    }

    private static void UpdateSceneBindings()
    {
        if (SceneBindings.Count == 0)
            return;

        foreach (var entry in SceneBindings.Where(kv => kv.Key.Contains("Scene@")))
            if (entry.Value.Source is EntityManager scene)
                UpdateBinding(scene, scene.GUID, SceneBindings);
    }

    private static void UpdateEntityBindings()
    {
        if (EntityBindings.Count == 0)
            return;

        Entity entity = EntityBindings.FirstOrDefault().Value.Source as Entity;

        UpdateBinding(entity, entity.GUID, EntityBindings);

        foreach (var component in entity.GetComponents())
            UpdateBinding(component, component.GetType().FullName + entity.GUID, EntityBindings);
    }

    private static void UpdateMaterialBindings()
    {
        if (MaterialBindings.Count == 0)
            return;

        foreach (var source in MaterialBindings.Select(kv => kv.Value.Source).ToArray())
            UpdateBinding(source, source.GetType().FullName, MaterialBindings);
    }

    private static void UpdateBinding(object source, object keySuffix, Dictionary<string, BindEntry> bindings)
    {
        if (source is null)
            return;

        var fields = source.GetType().GetFields(AllBindingFlags);
        foreach (var field in fields)
            foreach (var bindName in bindings.Keys)
                if (string.Equals(field.Name + keySuffix, bindName)
                 && bindings.TryGetValue(bindName, out var bindEntry)
                 && !Equals(field.GetValue(source), bindEntry.Value))
                    ProcessBindEntry(bindEntry, field, source);
    }
}

internal sealed partial class Binding
{
    /// <summary>
    /// Update the new Value from the Engine to the Editor.
    /// </summary>
    public static void ProcessBindEntry(BindEntry bindEntry, FieldInfo field, object source)
    {
        bindEntry.Value = field.GetValue(source);

        var fieldFromPath = bindEntry.Target?.GetType().GetField(bindEntry.TargetValuePath, AllBindingFlags);
        if (fieldFromPath is not null)
            fieldFromPath.SetValue(bindEntry.Target, bindEntry.Value);

        var propertyFromPath = bindEntry.Target?.GetType().GetProperty(bindEntry.TargetValuePath, AllBindingFlags);
        if (propertyFromPath is not null)
        {
            if (propertyFromPath.PropertyType == typeof(string))
                propertyFromPath.SetValue(bindEntry.Target, bindEntry.Value?.ToString());
            else
                propertyFromPath.SetValue(bindEntry.Target, bindEntry.Value);
        }

        bindEntry.Invoke();
    }

    public static void SetPathEvent(BindEntry bindEntry, BindingFlags bindingFlags)
    {
        var eventInfo = bindEntry.Target.GetType().GetEvent(bindEntry.TargetEventPath, bindingFlags);

        if (eventInfo is null)
            return;

        if (Equals(
            eventInfo.EventHandlerType,
            typeof(RoutedEventHandler)))
        {
            RoutedEventHandler handler = (s, e) =>
                EventLogic(s, bindEntry);

            eventInfo.AddEventHandler(bindEntry.Target, handler);
        }
        // TextBox
        else if (Equals(
            eventInfo.EventHandlerType,
            typeof(TypedEventHandler<TextBox, TextBoxTextChangingEventArgs>)))
        {
            TypedEventHandler<TextBox, TextBoxTextChangingEventArgs> handler = (s, e) =>
                EventLogic(s, bindEntry);

            eventInfo.AddEventHandler(bindEntry.Target, handler);
        }
        // NumberBox
        else if (Equals(
            eventInfo.EventHandlerType,
            typeof(TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>)))
        {
            TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs> handler = (s, e) =>
                EventLogic(s, bindEntry);

            eventInfo.AddEventHandler(bindEntry.Target, handler);
        }
        // Slider 
        else if (Equals(
            eventInfo.EventHandlerType,
            typeof(RangeBaseValueChangedEventHandler)))
        {
            RangeBaseValueChangedEventHandler handler = (s, e) =>
                EventLogic(s, bindEntry);

            eventInfo.AddEventHandler(bindEntry.Target, handler);
        }
        // ComboBox 
        else if (Equals(
            eventInfo.EventHandlerType,
            typeof(SelectionChangedEventHandler)))
        {
            SelectionChangedEventHandler handler = (s, e) =>
                EventLogic(s, bindEntry);

            eventInfo.AddEventHandler(bindEntry.Target, handler);
        }
        else
            throw new Exception(
                $"{eventInfo.EventHandlerType} was not considered in the Bindings Event Check!");

        //Output.Log($"Check Path Event: {eventInfo.Name}");
    }

    /// <summary>
    /// Update the new Value from the Editor to the Engine.
    /// </summary>
    public static void EventLogic(object sender, BindEntry bindEntry)
    {
        // Cast the sender object to the type specified by bindEntry.Target.GetType().
        var sourceType = bindEntry.Source.GetType();
        var sourceField = sourceType.GetField(bindEntry.SourceValuePath, AllBindingFlags);

        var targetType = bindEntry.Target.GetType();
        var targetField = targetType.GetProperty(bindEntry.TargetValuePath, AllBindingFlags);

        var castedSender = Convert.ChangeType(sender, targetType);
        var newValue = targetField.GetValue(castedSender);

        var convertedValue = bindEntry.Value switch
        {
            double => Convert.ToDouble(newValue),
            float => Convert.ToSingle(newValue),
            int => Convert.ToInt32(newValue),
            byte => Convert.ToByte(newValue),
            _ => newValue
        };

        if (sourceField.FieldType.IsEnum)
            if (Enum.TryParse(sourceField.FieldType, convertedValue.ToString(), out var enumValue))
                convertedValue = enumValue;
            else
                throw new ArgumentException("Failed to parse enum value");

        sourceField?.SetValue(bindEntry.Source, convertedValue);

        bindEntry.Value = convertedValue;

        // Invoke the original event handler, if it exists.
        bindEntry.Invoke();

        //Output.Log($"Handled Event: Value {convertedValue}");
    }
}
