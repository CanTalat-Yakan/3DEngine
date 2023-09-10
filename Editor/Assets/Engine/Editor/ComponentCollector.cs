using System.Collections.Generic;

namespace Engine.Editor;

internal sealed class ComponentCollector
{
    public List<Type> Components = new();

    public Type GetComponent(string name) =>
        Components.Find(Type => Type.Name.ToString() == name);
}
