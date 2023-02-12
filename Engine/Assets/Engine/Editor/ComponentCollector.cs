using System.Collections.Generic;
using System;

namespace Engine.Editor
{
    internal class ComponentCollector
    {
        public List<Type> Components = new();

        public void AddComponent(Type component) =>
            Components.Add(component);
        
        public void AddComponents(params Type[] components) =>
            Components.AddRange(components);

        public Type GetComponent(string name)
        {
            return Components.Find(Type => Type.Name.ToString() == name);
        }
    }
}
