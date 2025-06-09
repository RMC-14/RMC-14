using Content.Shared._RMC14.AegisCrate;
using Content.Shared.Storage;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Server.Storage.EntitySystems;
using Content.Server.Storage.Components;
using Robust.Shared.Timing;
using Robust.Shared.Maths;
using System.Numerics;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.Access.Systems;


namespace Content.Server._RMC14.AegisCrate;

public sealed class AegisCrateSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    private const float OpeningDuration = 1.2f; // seconds, match client animation

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AegisCrateComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AegisCrateComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<AegisCrateComponent, ComponentInit>(OnComponentInit);
    }

    private void OnStartup(EntityUid uid, AegisCrateComponent component, ComponentStartup args)
    {
        var x = MathF.Floor(Transform(uid).WorldPosition.X) + 0.5f;
        var y = MathF.Floor(Transform(uid).WorldPosition.Y) + 0.5f;
        Transform(uid).WorldPosition = new Vector2(x, y);
    }

    private void OnComponentInit(EntityUid uid, AegisCrateComponent comp, ComponentInit args)
    {
        comp.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(EntityUid uid, AegisCrateComponent comp)
    {
        if (comp.State == AegisCrateState.Open)
        {
            // Offset OB spawn slightly south
            var coords = Transform(uid).Coordinates.Offset(new Vector2(0, -0.2f));
            var ob = _entityManager.SpawnEntity("RMCOrbitalCannonWarheadAegis", coords);



            var tagSystem = EntitySystem.Get<TagSystem>();
            tagSystem.AddTag(ob, "RMCDropshipEnginePoint");

            Logger.Info($"OB spawned at {Transform(ob).WorldPosition}");
        }
    }

    private void OnInteractHand(EntityUid uid, AegisCrateComponent comp, InteractHandEvent args)
    {
        if (comp.State != AegisCrateState.Closed)
            return;

        if (!_accessReader.IsAllowed(args.User, uid))
        {
            return;
        }

        comp.State = AegisCrateState.Opening;
        Dirty(uid, comp);



        // Start a timer to set state to Open after animation duration
        Timer.Spawn(TimeSpan.FromSeconds(OpeningDuration), () =>
        {
            if (!Deleted(uid) && comp.State == AegisCrateState.Opening)
            {
                comp.State = AegisCrateState.Open;
                Dirty(uid, comp);
            }
        });
    }
}
