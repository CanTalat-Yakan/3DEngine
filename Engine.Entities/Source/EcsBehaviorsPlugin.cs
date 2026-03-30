namespace Engine;

/// <summary>Discovers and invokes source-generated behavior registration methods to wire systems into the app.</summary>
public sealed class BehaviorsPlugin : IPlugin
{
    public void Build(App app)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.IsDynamic) continue;
            try
            {
                var bindingFlags = System.Reflection.BindingFlags.Public |
                                   System.Reflection.BindingFlags.NonPublic |
                                   System.Reflection.BindingFlags.Static;
                var attributeType = typeof(GeneratedBehaviorRegistrationAttribute);
                foreach (var type in asm.GetTypes())
                foreach (var m in type.GetMethods(bindingFlags))
                {
                    if (m.GetCustomAttributes(attributeType, inherit: false).Length == 0) continue;
                    var ps = m.GetParameters();
                    if (ps.Length == 1 && ps[0].ParameterType == typeof(App))
                        m.Invoke(null, [app]);
                }
            }
            catch
            {
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class GeneratedBehaviorRegistrationAttribute : Attribute
{
}