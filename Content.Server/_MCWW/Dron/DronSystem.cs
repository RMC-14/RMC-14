
// using Content.Shared._MCWW.Dron;


// namespace Content.Server._MCWW.Dron;

// public sealed class DronSystem : EntitySystem
// {


//     /// <inheritdoc/>
//     public override void Initialize()
//     {
//         SubscribeLocalEvent<DronComponent, UseInHandEvent>(OnUseInHand);
//     }

//     private void OnUseInHand(Entity<DronComponent> ent, ref UseInHandEvent args)
//     {
//         if (args.Handled)
//             return;

//         TryInsert(uid, args.Args.User, component);
//         _actionBlocker.UpdateCanMove(uid);
//         args.Handled = true;
//     }
// }





//     // private void OnUseInHand(Entity<HyposprayComponent> entity, ref UseInHandEvent args)
//     // {
//     //     if (args.Handled)
//     //         return;

//     //     args.Handled = TryDoInject(entity, args.User, args.User);
//     // }
