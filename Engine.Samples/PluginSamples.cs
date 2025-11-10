// using SDL3;
//
// namespace Engine;
//
// public sealed class SamplePlugin : IPlugin
// {
//     public void Build(App app)
//     {
//         // Seed a few entities on startup for the demo
//         app.AddSystem(Stage.Startup, world =>
//         {
//             var ecs = world.Resource<EcsWorld>();
//             for (int i = 0; i < 3; i++)
//             {
//                 var e = ecs.Spawn();
//                 ecs.Add(e, new MyComponent { Value = i });
//             }
//         });
//
//         // Producer: increment component and publish a change event each frame
//         app.AddSystem(Stage.Update, world =>
//         {
//             var ecs = world.Resource<EcsWorld>();
//             var writer = EventWriter<MyComponentChanged>.Get(world);
//
//             foreach (var (e, comp) in ecs.Query<MyComponent>())
//             {
//                 // Query returns a copy of the value-type component.
//                 // Modify a local copy, then call Update to write back and mark it Changed this frame.
//                 var old = comp.Value;
//                 var next = comp;
//                 next.Value = old + 1;
//                 ecs.Update(e, next);
//
//                 writer.Send(new MyComponentChanged(e, old, next.Value));
//             }
//         });
//
//         // Input-driven: on Space pressed, spawn a new entity with a starting Value
//         app.AddSystem(Stage.Update, world =>
//         {
//             var ecs = world.Resource<EcsWorld>();
//             var pressedSpace = false;
//             foreach (var ev in EventReader<KeyPressed>.Get(world).Drain())
//             {
//                 if (ev.Scancode == Key.Space)
//                     pressedSpace = true;
//             }
//
//             if (pressedSpace)
//             {
//                 var e = ecs.Spawn();
//                 ecs.Add(e, new MyComponent { Value = 100 });
//             }
//         });
//
//         // Consumer: drain change events and log them (place after producer to get same-frame events)
//         app.AddSystem(Stage.PostUpdate, world =>
//         {
//             foreach (var ev in EventReader<MyComponentChanged>.Get(world).Drain())
//                 Console.WriteLine($"[Event] Entity {ev.EntityId}: Value {ev.OldValue} -> {ev.NewValue}");
//         });
//
//         // Alternate view: also demonstrate Changed<>() detection without relying on events.
//         app.AddSystem(Stage.PostUpdate, world =>
//         {
//             var ecs = world.Resource<EcsWorld>();
//             foreach (var (e, comp) in ecs.Query<MyComponent>())
//             {
//                 if (ecs.Changed<MyComponent>(e))
//                 {
//                     Console.WriteLine($"[Changed] Entity {e}: New Value {comp.Value}");
//                 }
//             }
//         });
//     }
// }
//
// /// <summary>Simple demo component. Using a struct keeps components value-type and easy to copy/update.</summary>
// public struct MyComponent
// {
//     public int Value;
// }
//
// /// <summary>Custom event showing how to publish domain events when components change.</summary>
// public readonly record struct MyComponentChanged(int EntityId, int OldValue, int NewValue);
