// using System.Linq;
// using Content.Server.Movement.Components;
// using Content.Shared._RMC14.Movement;
// using Content.Shared.Coordinates;
// using Content.Shared.Movement.Components;
// using Content.Shared.Movement.Systems;
// using Robust.Shared.GameObjects;
// using Robust.Shared.IoC;
// using Robust.Shared.Map;
// using Robust.Shared.Network;
// using Robust.Shared.Player;
// using Robust.Shared.Prototypes;
// using Robust.Shared.Serialization;
//
// namespace Content.IntegrationTests._RMC14;
//
// TODO RMC14 compare this to electro's
// [TestFixture]
// public sealed class RMCLagCompensationTest
// {
//     private static readonly EntProtoId Mob = "CMMobMoth";
//
//     [Test]
//     public async Task ClientServerMatch()
//     {
//         await using var pair = await PoolManager.GetServerClient(new PoolSettings
//         {
//             Connected = true,
//             DummyTicker = false,
//         });
//
//         var clientSystem = pair.Client.System<RMCLagCompensationTestSystem>();
//
//         var serverEntities = pair.Server.Resolve<IEntityManager>();
//         var serverSystem = pair.Server.System<RMCLagCompensationTestSystem>();
//         EntityUid serverSpawned = default;
//
//         await pair.RunSeconds(10);
//
//         await pair.Server.WaitAssertion(() =>
//         {
//             var player = pair.Server.PlayerMan.Sessions.First();
//             Assert.That(player.Ping, Is.GreaterThan(0));
//
//             if (player.AttachedEntity is not { } ent)
//             {
//                 Assert.Fail("No attached entity found for test client player");
//                 return;
//             }
//
//             serverSpawned = serverEntities.SpawnAtPosition(Mob, ent.ToCoordinates());
//             serverEntities.EnsureComponent<LagCompensationComponent>(serverSpawned);
//
//             serverEntities.EnsureComponent<InputMoverComponent>(serverSpawned).HeldMoveButtons = MoveButtons.Right;
//         });
//
//         await pair.RunSeconds(2);
//
//         await pair.Server.WaitAssertion(() =>
//         {
//             Assert.Multiple(() =>
//             {
//                 Assert.That(serverEntities.TryGetComponent(serverSpawned, out LagCompensationComponent? lagCompensation));
//                 Assert.That(lagCompensation, Is.Not.Null);
//                 Assert.That(lagCompensation.Positions, Has.Count.GreaterThanOrEqualTo(10));
//             });
//         });
//
//         await pair.Client.WaitAssertion(() =>
//         {
//             clientSystem.Run(pair.ToClientUid(serverSpawned));
//             Assert.That(clientSystem.Coordinates, Is.Not.Null);
//         });
//
//         await pair.RunSeconds(2);
//
//         await pair.Server.WaitAssertion(() =>
//         {
//             Assert.Multiple(() =>
//             {
//                 Assert.That(serverSystem.Coordinates, Is.Not.Null);
//                 Assert.That(clientSystem.Coordinates, Is.Not.Null);
//
//                 var clientCoords = clientSystem.Coordinates.Value;
//                 Assert.That(serverSystem.Coordinates, Is.EqualTo(new EntityCoordinates(pair.ToServerUid(clientCoords.EntityId), clientCoords.Position)));
//             });
//         });
//     }
//
//     public sealed class RMCLagCompensationTestSystem : EntitySystem
//     {
//         [Dependency] private readonly INetManager _net = default!;
//         [Dependency] private readonly ISharedPlayerManager _player = default!;
//         [Dependency] private readonly SharedRMCLagCompensationSystem _rmcLagCompensation = default!;
//
//         public EntityCoordinates? Coordinates;
//
//         public override void Initialize()
//         {
//             if (_net.IsServer)
//                 SubscribeNetworkEvent<RMCLagCompensationTestEvent>(OnEvent);
//         }
//
//         public override void Shutdown()
//         {
//             base.Shutdown();
//             Coordinates = null;
//         }
//
//         private void OnEvent(RMCLagCompensationTestEvent msg, EntitySessionEventArgs args)
//         {
//             if (args.SenderSession.AttachedEntity is not { } ent)
//                 return;
//
//             Coordinates ??= _rmcLagCompensation.GetCoordinates(GetEntity(msg.Target), ent);
//         }
//
//         public void Run(EntityUid target)
//         {
//             Coordinates = _rmcLagCompensation.GetCoordinates(target, _player.LocalEntity);
//             RaisePredictiveEvent(new RMCLagCompensationTestEvent(GetNetEntity(target)));
//         }
//     }
//
//     [Serializable, NetSerializable]
//     public sealed class RMCLagCompensationTestEvent(NetEntity target) : EntityEventArgs
//     {
//         public readonly NetEntity Target = target;
//     }
// }
