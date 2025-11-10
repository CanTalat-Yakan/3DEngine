namespace Engine;

/// <summary>Discovers and invokes source-generated behavior registration methods to wire systems into the app.</summary>
public sealed class BehaviorsPlugin : IPlugin
{
    public void Build(App app)
    {
        // Find all methods annotated with [GeneratedBehaviorRegistration] across loaded assemblies
        // and invoke them with the current App instance.
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Skip dynamic/reflection-only assemblies
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
                // Ignore type load/reflection issues from unrelated assemblies
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class GeneratedBehaviorRegistrationAttribute : Attribute
{
}