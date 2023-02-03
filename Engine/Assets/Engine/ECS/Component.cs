using Editor.Controller;
using Engine.Editor;
using System;

namespace Engine.ECS
{
    internal class Component : BindableBase, ICloneable
    {
        [Hide]
        public Entity Entity;
        [Hide]
        public byte Order = 0;

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }

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
