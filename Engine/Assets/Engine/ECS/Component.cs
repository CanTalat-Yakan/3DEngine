using System;

namespace Engine.ECS
{
    internal class Component : ICloneable
    {
        internal Entity Entity;

        public Component() => Register();

        public virtual void Register() { }

        public virtual void Awake() { }

        public virtual void Start() { }

        public virtual void Update() { }

        public virtual void LateUpdate() { }

        public virtual void Render() { }

        object ICloneable.Clone() { return Clone(); }

        public Component Clone() { return (Component)MemberwiseClone(); }
    }

    internal class EditorComponent : Component { }
}
