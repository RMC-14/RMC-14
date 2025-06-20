using Content.Shared._RMC14.AegisCrate;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Maths;
using System.Numerics;
using Content.Shared.Tag;
using Content.Shared.Access.Systems;

namespace Content.Server._RMC14.AegisCrate;

public sealed class AegisCrateSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;

    private const float OpeningDuration = 1.2f; // seconds, match client animation

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AegisCrateComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AegisCrateComponent, InteractHandEvent>(OnInteractHand);
    }

    private void OnStartup(EntityUid uid, AegisCrateComponent component, ComponentStartup args)
    {
        var worldPos = _transform.GetWorldPosition(uid);
        var x = MathF.Floor(worldPos.X) + 0.5f;
        var y = MathF.Floor(worldPos.Y) + 0.5f;
        _transform.SetWorldPosition(uid, new Vector2(x, y));

        // Trigger initial visual update
        UpdateCrateVisuals((uid, component));
    }

    private void UpdateState(EntityUid uid, AegisCrateComponent comp, AegisCrateState newState)
    {
        if (comp.State == newState)
            return;

        comp.State = newState;
        Dirty(uid, comp);

        // Update visuals after state change
        UpdateCrateVisuals((uid, comp));

        if (comp.State == AegisCrateState.Open)
        {
            // Offset OB spawn slightly south
            var coords = Transform(uid).Coordinates.Offset(new Vector2(0, -0.2f));
            var ob = _entityManager.SpawnEntity("RMCOrbitalCannonWarheadAegis", coords);

            _tagSystem.AddTag(ob, "RMCDropshipEnginePoint");

            Log.Info($"OB spawned at {_transform.GetWorldPosition(ob)}");
        }
    }

    private void UpdateCrateVisuals(Entity<AegisCrateComponent> crate)
    {
        var ev = new AegisCrateStateChangedEvent();
        RaiseLocalEvent(crate, ref ev);
    }

    private void OnInteractHand(EntityUid uid, AegisCrateComponent comp, InteractHandEvent args)
    {
        if (comp.State != AegisCrateState.Closed)
            return;

        if (!_accessReader.IsAllowed(args.User, uid))
        {
            return;
        }

        UpdateState(uid, comp, AegisCrateState.Opening);

        // Start a timer to set state to Open after animation duration
        Timer.Spawn(TimeSpan.FromSeconds(OpeningDuration), () =>
        {
            if (!Deleted(uid) && comp.State == AegisCrateState.Opening)
            {
                UpdateState(uid, comp, AegisCrateState.Open);
            }
        });
    }
}
