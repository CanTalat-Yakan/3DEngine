using System.Collections.Generic;
using System.Linq;

namespace Engine.Editor;

public record BindEntry(object Object, EventHandler Event = null);

public class Binding
{
    public static Dictionary<string, BindEntry> SceneBindings = new();
    public static Dictionary<string, BindEntry> EntityBindings = new();

    public static void SetBinding(Scene scene)
    {
        SceneBindings.Add("Scene@" + scene.ID, new(scene));
        SceneBindings.Add("Name" + scene.Name, new(scene.Name));
        SceneBindings.Add("IsEnabled" + scene.Name, new(scene.IsEnabled));
    }

    public static void SetBinding(Entity entity)
    {
        EntityBindings.Add("Entity", new(entity));
        EntityBindings.Add("Name", new(entity.Name));
        EntityBindings.Add("IsStatic", new(entity.IsStatic));
        EntityBindings.Add("IsEnabled", new(entity.IsEnabled));
        EntityBindings.Add("Layer", new(entity.Layer));
        EntityBindings.Add("Tag", new(entity.Tag));

        foreach (var component in entity.Components)
            foreach (var field in component.GetType().GetFields())
                EntityBindings.Add(field.Name, new(field));
    }

    public static void Clear(Dictionary<object, EventHandler> dictionary) =>
        dictionary.Clear();

    public static void Update()
    {
        Entity entity = EntityBindings["Entity"].Object as Entity;

        foreach (var field in entity.GetType().GetFields())
        {
            EntityBindings.TryGetValue(field.Name, out var bindEntry);

            if (bindEntry is not null)
                if (Equals(field, bindEntry.Object))
                    bindEntry.Event?.Invoke(null, null);
        }

        var allComponentFields = entity.Components
            .SelectMany(component => component.GetType().GetFields());
        foreach (var field in allComponentFields)
        {
            EntityBindings.TryGetValue(field.Name, out var bindEntry);

            if (bindEntry is not null)
                if (Equals(field, bindEntry.Object))
                    bindEntry.Event?.Invoke(null, null);
        }

        Scene[] scenes = SceneBindings
            .Where(kv => kv.Key.Contains("Scene@"))
            .Select(kv => kv.Value)
            .Select(entry => entry.Object as Scene)
            .ToArray();
        foreach (var scene in scenes)
            foreach (var field in scene.GetType().GetFields())
            {
                SceneBindings.TryGetValue("Scene" + scene.Name, out var bindEntry);

                if (bindEntry is not null)
                    if (Equals(field, bindEntry.Object))
                        bindEntry.Event?.Invoke(null, null);
            }
    }
}

//EntityBindings["Name"] = (s, e) => Core.Instance.Frame();
