using Content.Shared._RMC14.SpecialBox;
using Content.Shared.Storage;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Content.Server.Storage.EntitySystems;
using Content.Server.Storage.Components;
using Content.Shared.Access.Systems;    // Keep this line, remove Content.Server.Access.Systems if present
using Robust.Shared.Timing;
using Robust.Shared.Maths;
using System.Numerics;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Content.Shared.Tag;

namespace Content.Server._RMC14.SpecialBox;

public sealed class SpecialBoxSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    private const float OpeningDuration = 1.2f; // seconds, match client animation

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SpecialBoxComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<SpecialBoxComponent, InteractHandEvent>(OnInteractHand);
        // Subscribe to state change event
        SubscribeLocalEvent<SpecialBoxComponent, ComponentInit>(OnComponentInit);
    }

    private void OnStartup(EntityUid uid, SpecialBoxComponent component, ComponentStartup args)
    {
        var x = MathF.Floor(Transform(uid).WorldPosition.X) + 0.5f;
        var y = MathF.Floor(Transform(uid).WorldPosition.Y) + 0.5f;
        Transform(uid).WorldPosition = new Vector2(x, y);
    }

    private void OnComponentInit(EntityUid uid, SpecialBoxComponent comp, ComponentInit args)
    {
        comp.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(EntityUid uid, SpecialBoxComponent comp)
    {
        if (comp.State == SpecialBoxState.Open)
        {
            // Offset OB spawn slightly south (down)
            var coords = Transform(uid).Coordinates.Offset(new Vector2(0, -0.2f));
            var ob = _entityManager.SpawnEntity("RMCOrbitalCannonWarheadCluster", coords);

            // Add the tag so only this OB is interactable from range


            var tagSystem = EntitySystem.Get<TagSystem>();
            tagSystem.AddTag(ob, "RMCDropshipEnginePoint");

            Logger.Info($"OB spawned at {Transform(ob).WorldPosition}");
        }
    }

    private void OnInteractHand(EntityUid uid, SpecialBoxComponent comp, InteractHandEvent args)
    {
        if (comp.State != SpecialBoxState.Closed)
            return;

        if (!_accessReader.IsAllowed(args.User, uid))
        {
            // Optionally send a popup message to the user
            return;
        }

        comp.State = SpecialBoxState.Opening;
        Dirty(uid, comp);

        // Start a timer to set state to Open after animation duration
        Timer.Spawn(TimeSpan.FromSeconds(OpeningDuration), () =>
        {
            if (!Deleted(uid) && comp.State == SpecialBoxState.Opening)
            {
                comp.State = SpecialBoxState.Open;
                Dirty(uid, comp);
            }
        });
    }
}
