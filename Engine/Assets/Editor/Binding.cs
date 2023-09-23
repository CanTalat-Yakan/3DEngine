using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Engine.Editor;

public class BindEntry(object source, string sourcePath)
{
    public object Source = source;
    public string SourcePath = sourcePath;

    public object Target;
    public string TargetPath;

    public string PathEvent;
    public EventHandler Event;

    public void Set(object target, string targetPath, string pathEvent = null)
    {
        Target = target;
        TargetPath = targetPath;
        PathEvent = pathEvent;
    }
}

public class Binding
{
    public static Dictionary<string, BindEntry> SceneBindings = new();
    public static Dictionary<string, BindEntry> EntityBindings = new();

    public static void SetBinding(Scene scene)
    {
        SceneBindings.Add("Scene@" + scene.ID, new(scene, "Scene@"));
        SceneBindings.Add("Name" + scene.ID, new(scene, "Name"));
        SceneBindings.Add("IsEnabled" + scene.ID, new(scene, "IsEnabled"));
    }

    public static void SetBinding(Entity entity)
    {
        EntityBindings.Add("Entity@" + entity.ID, new(entity, "Entity@"));
        EntityBindings.Add("Name" + entity.ID, new(entity, "Name"));
        EntityBindings.Add("IsStatic" + entity.ID, new(entity, "IsStatic"));
        EntityBindings.Add("IsEnabled" + entity.ID, new(entity, "IsEnabled"));

        var components = entity.Components;
        for (int i = 1; i < components.Count; i++) // Skip transform.
            foreach (var field in components[i].GetType().GetFields())
                EntityBindings.Add(field.Name + components[i], new(components[i], field.Name));
    }

    public static void Update()
    {
        UpdateEntityBindings();
        UpdateSceneBindings();
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
                        bindEntry.Source.GetType().GetField(bindEntry.SourcePath)))
                    InvokeBindEntry(bindEntry, field, source);
    }

    public static void InvokeBindEntry(BindEntry bindEntry, FieldInfo field, object source)
    {
        bindEntry.Source = field.GetValue(source);

        BindingFlags bindingFlags =
            BindingFlags.NonPublic |
            BindingFlags.Public |
            BindingFlags.Static |
            BindingFlags.Instance;

        var fieldFromPath = bindEntry.Target?.GetType().GetField(bindEntry.TargetPath, bindingFlags);
        if (fieldFromPath is not null)
            fieldFromPath.SetValue(bindEntry.Target, bindEntry.Source);

        var propertyFromPath = bindEntry.Target?.GetType().GetProperty(bindEntry.TargetPath, bindingFlags);
        if (propertyFromPath is not null)
            propertyFromPath.SetValue(bindEntry.Target, bindEntry.Source);

        if (!string.IsNullOrEmpty(bindEntry.PathEvent))
            CheckPathEvent(bindEntry, source, bindingFlags);

        bindEntry.Event?.Invoke(null, null);
    }

    public static void CheckPathEvent(BindEntry bindEntry, object source, BindingFlags bindingFlags)
    {
        var eventInfo = source.GetType().GetEvent(bindEntry.PathEvent, bindingFlags);
        if (eventInfo is null)
            return;

        // Create a new delegate that incorporates the dynamic logic.
        EventHandler handler = (s, e) =>
        {
            Output.Log("Handled Event Dynamic");

            // Cast the sender object to the type specified by bindEntry.Target.GetType().
            var targetType = bindEntry.Target.GetType();
            var castedSender = Convert.ChangeType(s, targetType);

            var newValue = castedSender.GetType().GetProperty(bindEntry.TargetPath);
            bindEntry.Source.GetType().GetField(bindEntry.SourcePath, bindingFlags)?
                .SetValue(castedSender, newValue);

            bindEntry.Source = newValue;
            // You need the reference of the class of the field you want to change with new sender
            // Then with the target that is togglebutton and the castedSender contains the new value
            // The new value is accessable with the Path and inside the Properties


            // Invoke the original event handler, if it exists.
            bindEntry.Event?.Invoke(s, e);
        };

        // Add the new delegate as an event handler.
        eventInfo.AddEventHandler(source, handler);
    }

    public static void Clear(Dictionary<string, BindEntry> dictionary) =>
        dictionary.Clear();

    public static void Remove(Guid guid)
    {
        foreach (var bind in SceneBindings.ToArray())
            if (bind.Key.Contains(guid.ToString()))
                SceneBindings.Remove(bind.Key);
    }
}
