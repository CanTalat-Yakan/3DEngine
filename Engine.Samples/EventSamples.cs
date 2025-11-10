// namespace Engine;
//
// /// <summary>Event: example event struct representing damage dealt.</summary>
// public readonly record struct DamageEvent(int entityId, int amount);
//
// [Behavior]
// public struct DamageDealer
// {
//     [OnUpdate]
//     public static void Deal(BehaviorContext ctx)
//     {
//         //One event per tick for this entity (example) 
//         EventWriter<DamageEvent>.Get(ctx.World).Send(new DamageEvent(ctx.EntityId, 1));
//     }
// }
//
// [Behavior]
// public struct SpaceToggle
// {
//     [OnUpdate]
//     public static void OnKey(BehaviorContext ctx)
//     {
//         foreach (var evt in EventReader<KeyPressed>.Get(ctx.World).Drain())
//             if (evt.Scancode == Key.Space)
//                 Console.WriteLine("Space pressed!");
//     }
// }