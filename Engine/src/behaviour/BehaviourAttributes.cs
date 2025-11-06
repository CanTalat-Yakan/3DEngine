using System;

namespace Engine.Behaviour
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public sealed class BehaviourAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class OnStartupAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class OnFirstAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class OnPreUpdateAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class OnUpdateAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class OnPostUpdateAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class OnRenderAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class OnLastAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class WithAttribute : Attribute
    {
        public Type[] Types { get; }
        public WithAttribute(params Type[] types) => Types = types;
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class WithoutAttribute : Attribute
    {
        public Type[] Types { get; }
        public WithoutAttribute(params Type[] types) => Types = types;
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class ChangedAttribute : Attribute
    {
        public Type[] Types { get; }
        public ChangedAttribute(params Type[] types) => Types = types;
    }
}
