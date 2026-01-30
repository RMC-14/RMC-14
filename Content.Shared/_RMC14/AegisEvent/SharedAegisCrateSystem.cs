using Content.Shared.Interaction;
using Robust.Shared.Timing;
using Content.Shared.Access.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Content.Shared.Coordinates;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Physics;
using System.Linq;
using Content.Shared.Physics;

namespace Content.Shared._RMC14.AegisCrate;

public abstract class SharedAegisCrateSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    protected readonly TimeSpan OpeningSpeed = TimeSpan.FromSeconds(1.5);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AegisCrateComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AegisCrateComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<AegisCrateComponent, InteractUsingEvent>(OnInteractUsing);
    }

    protected virtual void OnStartup(Entity<AegisCrateComponent> crate, ref ComponentStartup args)
    {
        // Trigger initial visual update
        UpdateCrateVisuals(crate);
    }

    private void UpdateState(Entity<AegisCrateComponent> crate, AegisCrateState newState)
    {
        if (crate.Comp.State == newState)
            return;

        crate.Comp.State = newState;
        Dirty(crate);

        // Update visuals after state change
        UpdateCrateVisuals(crate);
    }

    private void UpdateCrateVisuals(Entity<AegisCrateComponent> crate)
    {
        var ev = new AegisCrateStateChangedEvent();
        RaiseLocalEvent(crate, ref ev);
    }

    private void OpenAegis(Entity<AegisCrateComponent> crate, EntityUid user)
    {
        if (crate.Comp.State != AegisCrateState.Closed)
            return;

        if (!_accessReader.IsAllowed(user, crate))
            return;

        UpdateState(crate, AegisCrateState.Opening);

        _audio.PlayPredicted(crate.Comp.OpenSound, crate, user);
        crate.Comp.OpenAt = _timing.CurTime + OpeningSpeed;
    }

    private void OnActivate(Entity<AegisCrateComponent> crate, ref ActivateInWorldEvent args)
    {
        OpenAegis(crate, args.User);
    }

    private void OnInteractUsing(Entity<AegisCrateComponent> crate, ref InteractUsingEvent args)
    {
        OpenAegis(crate, args.User);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<AegisCrateComponent>();

        while(query.MoveNext(out var uid, out var aegis))
        {
            if (aegis.Spawned || aegis.OpenAt == null || time < aegis.OpenAt)
                continue;

            if (!TryComp<FixturesComponent>(uid, out var fixture))
                continue;

            UpdateState((uid, aegis), AegisCrateState.Open);

            var fix = fixture.Fixtures.First();

            _physics.SetCollisionLayer(uid, fix.Key, fix.Value, (int)(CollisionGroup.Opaque | CollisionGroup.BulletImpassable), manager: fixture);

            aegis.Spawned = true;
            Dirty(uid, aegis);

            var coords = uid.ToCoordinates();
            var ob = SpawnAtPosition(aegis.OB, coords);

            Log.Info($"{ob.Id} spawned at {_transform.GetWorldPosition(ob)}");

        }
    }
}
