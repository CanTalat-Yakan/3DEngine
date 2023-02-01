using Editor.Controller;
using System;

namespace Engine.ECS
{
    internal class Component : BindableBase, ICloneable
    {
        internal Entity _entity; 
        
        public bool IsActive
        {
            get { return _isActive; }
            set { SetProperty(ref _isActive, value); }
        }
        private bool _isActive;

        internal event EventHandler _eventOnDestroy;

        public Component() =>
            OnRegister();

        public virtual void OnRegister() { }

        public virtual void OnAwake() { }

        public virtual void OnStart() { }

        public virtual void OnUpdate() { }

        public virtual void OnLateUpdate() { }

        public virtual void OnRender() { }

        public virtual void OnDestroy() { }

        public void InvokeEventOnDestroy() =>
            _eventOnDestroy(this, null);

        object ICloneable.Clone() { return Clone(); }

        public Component Clone() { return (Component)MemberwiseClone(); }
    }

    internal class EditorComponent : Component { }
}
