namespace Engine;

/// <summary>Discovers and invokes source-generated behavior registration methods to wire systems into the app.</summary>
public sealed class BehaviorsPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.Behaviors");

    public void Build(App app)
    {
        Logger.Info("BehaviorsPlugin: Scanning assemblies for generated behavior registrations...");
        int found = 0;
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
                    {
                        Logger.Debug($"  Invoking behavior registration: {type.Name}.{m.Name}");
                        m.Invoke(null, [app]);
                        found++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"  Failed to scan assembly {asm.GetName().Name}: {ex.Message}");
            }
        }
        Logger.Info($"BehaviorsPlugin: {found} behavior registration(s) discovered and invoked.");
    }
}
