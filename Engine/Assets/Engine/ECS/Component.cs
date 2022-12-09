namespace Engine.ECS
{
    internal class Component
    {
        public Entity Entity;

        public virtual void Awake() { }

        public virtual void Start() { }

        public virtual void Update() { }

        public virtual void LateUpdate() { }

        public virtual void Render() { }
    }
}
