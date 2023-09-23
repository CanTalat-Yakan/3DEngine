using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;

using Engine.Utilities;
using Engine.ECS;

namespace Editor.Controller;

internal class BindEntry(object source, string sourcePath)
{
    public object Value;

    public object Source = source;
    public string SourcePath = sourcePath;

    public object Target;
    public string TargetPath;

    public string PathEvent;
    public EventHandler Event;
    public object TargetPathEventType;

    public void Set(object target, string targetPath, string pathEvent = null, object targetPathEventType = null)
    {
        Target = target;
        TargetPath = targetPath;
        PathEvent = pathEvent;
        TargetPathEventType = targetPathEventType;
    }
}

internal class Binding
{
    public static Dictionary<string, BindEntry> SceneBindings = new();
    public static Dictionary<string, BindEntry> EntityBindings = new();

    public static void Update()
    {
        SetBinding(Engine.Editor.Binding.DequeueAddedScenes());
        Remove(Engine.Editor.Binding.DequeueRemovedScenes());

        UpdateEntityBindings();
        UpdateSceneBindings();
    }

    public static void SetBinding(Scene scene)
    {
        if (scene is null)
            return;

        SceneBindings.Add("Scene@" + scene.ID, new(scene, "Scene@"));
        SceneBindings.Add("Name" + scene.ID, new(scene, "Name"));
        SceneBindings.Add("IsEnabled" + scene.ID, new(scene, "IsEnabled"));
    }

    public static void SetBinding(Entity entity)
    {
        if (entity is null)
            return;

        EntityBindings.Add("Entity@" + entity.ID, new(entity, "Entity@"));
        EntityBindings.Add("Name" + entity.ID, new(entity, "Name"));
        EntityBindings.Add("IsStatic" + entity.ID, new(entity, "IsStatic"));
        EntityBindings.Add("IsEnabled" + entity.ID, new(entity, "IsEnabled"));

        var components = entity.Components;
        for (int i = 1; i < components.Count; i++) // Skip transform.
            foreach (var field in components[i].GetType().GetFields())
                EntityBindings.Add(field.Name + components[i], new(components[i], field.Name));
    }

    private static void UpdateEntityBindings()
    {
        if (EntityBindings.Count == 0)
            return;

        Entity entity = EntityBindings.FirstOrDefault().Value.Source as Entity;

        UpdateBindings(entity, entity.ID, EntityBindings);
        UpdateComponentBindings(entity);
    }

    private static void UpdateComponentBindings(Entity entity)
    {
        var components = entity.Components;

        for (int i = 1; i < components.Count; i++) // Skip transform.
            UpdateBindings(components[i], components[i], EntityBindings);
    }

    private static void UpdateSceneBindings()
    {
        if (SceneBindings.Count == 0)
            return;

        foreach (var entry in SceneBindings.Where(kv => kv.Key.Contains("Scene@")))
            if (entry.Value.Source is Scene scene)
                UpdateBindings(scene, scene.ID, SceneBindings);
    }

    private static void UpdateBindings(object source, object sufix, Dictionary<string, BindEntry> bindings)
    {
        foreach (var field in source.GetType().GetFields())
            foreach (var bindName in bindings.Keys)
                if (string.Equals(field.Name + sufix, bindName) &&
                    bindings.TryGetValue(bindName, out var bindEntry) &&
                    !Equals(
                        field.GetValue(source),
                        bindEntry.Value))
                    InvokeBindEntry(bindEntry, field, source);
    }

    public static void InvokeBindEntry(BindEntry bindEntry, FieldInfo field, object source)
    {
        bindEntry.Value = field.GetValue(source);

        BindingFlags bindingFlags =
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.Instance;

        var fieldFromPath = bindEntry.Target?.GetType().GetField(bindEntry.TargetPath, bindingFlags);
        if (fieldFromPath is not null)
            fieldFromPath.SetValue(bindEntry.Target, bindEntry.Value);

        var propertyFromPath = bindEntry.Target?.GetType().GetProperty(bindEntry.TargetPath, bindingFlags);
        if (propertyFromPath is not null)
            propertyFromPath.SetValue(bindEntry.Target, bindEntry.Value);

        if (!string.IsNullOrEmpty(bindEntry.PathEvent))
            CheckPathEvent(bindEntry, bindingFlags);

        bindEntry.Event?.Invoke(null, null);
    }

    public static void CheckPathEvent(BindEntry bindEntry, BindingFlags bindingFlags)
    {
        var eventInfo = bindEntry.Target.GetType().GetEvent(bindEntry.PathEvent, bindingFlags);
        Output.Log($"Check Path Event {eventInfo}");
        if (eventInfo is null)
            return;

        // Create a new delegate that incorporates the dynamic logic.
        RoutedEventHandler handler = (s, e) =>
        {
            Output.Log("Handled Event Dynamic");

            // Cast the sender object to the type specified by bindEntry.Target.GetType().
            var targetType = bindEntry.Target.GetType();
            var castedSender = Convert.ChangeType(s, targetType);

            var newValue = castedSender.GetType().GetProperty(bindEntry.TargetPath).GetValue(castedSender);

            bindEntry.Source.GetType().GetField(bindEntry.SourcePath, bindingFlags)?
                .SetValue(bindEntry.Source, newValue);

            bindEntry.Value = newValue;

            // Invoke the original event handler, if it exists.
            bindEntry.Event?.Invoke(null, null);
        };

        // Add the new delegate as an event handler.
        eventInfo.AddEventHandler(bindEntry.Target, handler);
    }

    public static void Clear(Dictionary<string, BindEntry> dictionary)
    {
        if (dictionary is not null)
            dictionary.Clear();
    }

    public static void Remove(Guid? guid)
    {
        if (guid is not null)
            foreach (var bind in SceneBindings.ToArray())
                if (bind.Key.Contains(guid.ToString()))
                    SceneBindings.Remove(bind.Key);
    }
}
