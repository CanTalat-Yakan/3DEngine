using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System;
using Windows.Foundation;

using Engine.Editor;
using Engine.ECS;
using Engine.Utilities;

namespace Editor.Controller;

internal class BindEntry(object source, string sourcePath)
{
    public object Value;

    public object Source = source;
    public string SourceValuePath = sourcePath;

    public object Target;
    public string TargetValuePath;
    public string TargetEventPath;

    public EventHandler Event;

    public void Set(object target, string targetValuePath, string targetEventPath = null)
    {
        Target = target;
        TargetValuePath = targetValuePath;
        TargetEventPath = targetEventPath;
    }

    public void SetEvent(EventHandler eventHandler) =>
        Event += eventHandler;
}

internal class Binding
{
    // Key = Field + Component.GetType().FullName
    public static Dictionary<string, BindEntry> RendererBindings = new();
    // Key = Field + Scene.ID
    public static Dictionary<string, BindEntry> SceneBindings = new();
    // Key = Field + Entity.ID
    // Key = Field + Component.GetType().FullName + Entity.ID
    public static Dictionary<string, BindEntry> EntityBindings = new();

    public static BindingFlags AllBindingFlags =
        BindingFlags.NonPublic |
        BindingFlags.Public |
        BindingFlags.Static |
        BindingFlags.Instance;

    public static void Update()
    {
        UpdateRendererBindings();
        UpdateSceneBindings();
        UpdateEntityBindings();
    }

    public static void SetRendererBindings()
    {
        RendererBindings.Add(
            "FOV" + ViewportController.Camera?.GetType(),
            new(ViewportController.Camera, "FOV"));
    }

    public static void SetBindings(Scene scene)
    {
        if (scene is null)
            return;

        SceneBindings.Add("Scene@" + scene.ID, new(scene, "Scene@"));
        SceneBindings.Add("Name" + scene.ID, new(scene, "Name"));
        SceneBindings.Add("IsEnabled" + scene.ID, new(scene, "IsEnabled"));
    }

    public static void SetBindings(Entity entity)
    {
        if (entity is null)
            return;

        EntityBindings.Add("Entity@" + entity.ID, new(entity, "Entity@"));
        EntityBindings.Add("Name" + entity.ID, new(entity, "Name"));
        EntityBindings.Add("IsStatic" + entity.ID, new(entity, "IsStatic"));
        EntityBindings.Add("IsEnabled" + entity.ID, new(entity, "IsEnabled"));

        foreach (var component in entity.Components.ToArray())
            foreach (var field in component.GetType().GetFields(AllBindingFlags))
                EntityBindings.Add(field.Name + component.GetType().FullName + entity.ID, new(component, field.Name));
    }

    /// <summary>
    /// [Renderer Key = Field.Name + Component.GetType().FullName]   
    /// [Scene Key = Field.Name + Scene.ID]  
    /// [Entity Key = Field.Name + Entity.ID]    
    /// [Component Key = Field.Name + Component.GetType().FullName + Entity.ID]  
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

        return null;
    }

    public static BindEntry GetBinding(string fieldName, object component, object entityID)
    {
        return entityID is not null
            ? GetEntityBinding(fieldName, component, entityID)
            : GetRendererBinding(fieldName, component);
    }

    public static BindEntry GetRendererBinding(string fieldName, object component) =>
        Get(fieldName + component?.GetType().FullName, RendererBindings);

    public static BindEntry GetSceneBinding(string fieldName, object sceneID) =>
        Get(fieldName + sceneID, SceneBindings);

    public static BindEntry GetEntityBinding(string fieldName, object component, object entityID) =>
        Get(fieldName + component?.GetType().FullName + entityID, EntityBindings);

    public static BindEntry GetEntityBinding(string fieldName, object entityID) =>
        Get(fieldName + entityID, EntityBindings);

    private static void UpdateSceneBindings()
    {
        if (SceneBindings.Count == 0)
            return;

        foreach (var entry in SceneBindings.Where(kv => kv.Key.Contains("Scene@")))
            if (entry.Value.Source is Scene scene)
                UpdateBinding(scene, scene.ID, SceneBindings);
    }

    private static void UpdateRendererBindings()
    {
        if (RendererBindings.Count == 0)
            return;

        foreach (var source in RendererBindings.Select(kv => kv.Value.Source).ToArray())
            UpdateBinding(source, source, RendererBindings);
    }

    private static void UpdateEntityBindings()
    {
        if (EntityBindings.Count == 0)
            return;

        Entity entity = EntityBindings.FirstOrDefault().Value.Source as Entity;

        UpdateBinding(entity, entity.ID, EntityBindings);

        foreach (var component in entity.Components.ToArray())
            UpdateBinding(component, component.GetType().FullName + entity.ID, EntityBindings);
    }

    private static void UpdateBinding(object source, object keySufix, Dictionary<string, BindEntry> bindings)
    {
        if (source is not null)
            foreach (var field in source.GetType().GetFields(AllBindingFlags))
                foreach (var bindName in bindings.Keys)
                    if (string.Equals(field.Name + keySufix, bindName)
                        && bindings.TryGetValue(bindName, out var bindEntry)
                        && !Equals(field.GetValue(source), bindEntry.Value))
                        ProcessBindEntry(bindEntry, field, source);
    }

    public static void ProcessBindEntry(BindEntry bindEntry, FieldInfo field, object source)
    {
        bindEntry.Value = field.GetValue(source);

        var fieldFromPath = bindEntry.Target?.GetType().GetField(bindEntry.TargetValuePath, AllBindingFlags);
        if (fieldFromPath is not null)
            fieldFromPath.SetValue(bindEntry.Target, bindEntry.Value);

        var propertyFromPath = bindEntry.Target?.GetType().GetProperty(bindEntry.TargetValuePath, AllBindingFlags);
        if (propertyFromPath is not null)
            propertyFromPath.SetValue(bindEntry.Target, bindEntry.Value);

        if (!string.IsNullOrEmpty(bindEntry.TargetEventPath))
            CheckPathEvent(bindEntry, AllBindingFlags);

        bindEntry.Event?.Invoke(null, null);
    }

    public static void CheckPathEvent(BindEntry bindEntry, BindingFlags bindingFlags)
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
        else
            Trace.WriteLine(
                $"{eventInfo.EventHandlerType} was not considered in the Bindings Event Check!");

        //Output.Log($"Check Path Event: {eventInfo.Name}");
    }

    public static void EventLogic(object sender, BindEntry bindEntry)
    {
        // Cast the sender object to the type specified by bindEntry.Target.GetType().
        var targetType = bindEntry.Target.GetType();
        var castedSender = Convert.ChangeType(sender, targetType);

        var newValue = targetType.GetProperty(bindEntry.TargetValuePath).GetValue(castedSender);

        newValue = bindEntry.Value switch
        {
            float => Convert.ToSingle(newValue),
            int => Convert.ToInt32(newValue),
            byte => Convert.ToByte(newValue),
            _ => newValue
        };

        bindEntry.Source.GetType().GetField(bindEntry.SourceValuePath, AllBindingFlags)?
            .SetValue(bindEntry.Source, newValue);

        bindEntry.Value = newValue;

        // Invoke the original event handler, if it exists.
        bindEntry.Event?.Invoke(null, null);

        //Output.Log($"Handled Event: Value {newValue}");
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
}
