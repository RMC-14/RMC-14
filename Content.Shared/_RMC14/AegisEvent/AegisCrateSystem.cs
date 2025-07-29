using Content.Shared._RMC14.AegisCrate;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using System.Numerics;
using Content.Shared.Tag;
using Content.Shared.Access.Systems;

namespace Content.Shared._RMC14.AegisCrate;

public sealed class AegisCrateSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly TimeSpan OpeningDuration = TimeSpan.FromSeconds(1.2); // match client animation

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AegisCrateComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AegisCrateComponent, InteractUsingEvent>(OnInteractUsing);
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

    private void UpdateState(Entity<AegisCrateComponent> crate, AegisCrateState newState)
    {
        if (crate.Comp.State == newState)
            return;

        crate.Comp.State = newState;
        Dirty(crate);

        // Update visuals after state change
        UpdateCrateVisuals(crate);

        if (crate.Comp.State == AegisCrateState.Open)
        {
            // Offset OB spawn slightly south
            var coords = Transform(crate).Coordinates.Offset(new Vector2(0, -0.2f));
            var ob = SpawnAtPosition(crate.Comp.OB, coords);

            Log.Info($"AEGIS OB spawned at {_transform.GetWorldPosition(ob)}");
        }
    }

    private void UpdateCrateVisuals(Entity<AegisCrateComponent> crate)
    {
        var ev = new AegisCrateStateChangedEvent();
        RaiseLocalEvent(crate, ref ev);
    }

    private void OnInteractUsing(Entity<AegisCrateComponent> crate, ref InteractUsingEvent args)
    {
        if (crate.Comp.State != AegisCrateState.Closed)
            return;

        if (!_accessReader.IsAllowed(args.User, crate))
        {
            return;
        }

        UpdateState(crate, AegisCrateState.Opening);

        // Start a timer to set state to Open after animation duration
        crate.Comp.OpenAt = _timing.CurTime + OpeningDuration;
    }

    public override void Update(float frameTime)
    {
        var crateQuery = EntityQueryEnumerator<AegisCrateComponent>();

        while (crateQuery.MoveNext(out var uid, out var crate))
        {
            if (crate.OpenAt == null)
                continue;

            if (crate.State == AegisCrateState.Opening)
                UpdateState((uid, crate), AegisCrateState.Open);

            crate.OpenAt = null;
            Dirty(uid, crate);
        }
    }
}
