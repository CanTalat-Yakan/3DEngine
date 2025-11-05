namespace Engine;

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
                world.Add(e, new Position { X = 0, Y = 0 });
            });
        });

        app.AddSystem(Stage.Update, (World w) =>
        {
            var t = w.Resource<Time>();
            var ecs = w.Resource<EcsWorld>();
            var cmds = w.Resource<EcsCommands>();
            foreach (var (entity, p) in ecs.Query<Position>())
            {
                var pos = p;
                pos.X += (float)(10 * t.DeltaSeconds);
                pos.Y += (float)(5 * t.DeltaSeconds);
                cmds.Add(entity, pos);
            }
        });
    }
}
