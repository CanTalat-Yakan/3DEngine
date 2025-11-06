namespace Engine;

public struct MinimalComponent { public float X, Y; }

public sealed class MinimalExample : IPlugin
{
    public void Build(App app)
    {
        app.AddPlugin(new ECSPlugin());

        app.AddSystem(Stage.Startup, (World w) =>
        {
            Console.WriteLine("[Startup] MinimalExample initialized");
            var cmds = w.Resource<EcsCommands>();
            cmds.Spawn((e, world) =>
            {
                world.Add(e, new MinimalComponent { X = 0, Y = 0 });
            });
        });

        app.AddSystem(Stage.Update, (World w) =>
        {
            var t = w.Resource<Time>();
            var ecs = w.Resource<EcsWorld>();
            var cmds = w.Resource<EcsCommands>();
            foreach (var (entity, c) in ecs.Query<MinimalComponent>())
            {
                var comp = c;
                comp.X += (float)(10 * t.DeltaSeconds);
                comp.Y += (float)(5 * t.DeltaSeconds);
                cmds.Add(entity, comp);
            }
        });
    }
}
