using Content.Shared._RMC14.Projectiles;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Tether;

public abstract partial class SharedRMCTetherSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCTetherComponent, ProjectileShotEvent>(OnProjectileShot);
    }

    private void OnProjectileShot(Entity<RMCTetherComponent> ent, ref ProjectileShotEvent ev)
    {
        if (ev.Predicted && _net.IsServer)
            ent.Comp.VisibleToOrigin = false;

        ent.Comp.TetherOrigin = ev.Shooter;

        if (ev.Shooter != null)
            ent.Comp.StaticTetherOrigin = _transform.GetMapCoordinates(ev.Shooter.Value);

        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<RMCTetherComponent>();

        while (query.MoveNext(out var uid, out var tether))
        {
            if (tether.RemoveAt is not { } removeAt)
                continue;

            if (_timing.CurTime > removeAt)
                RemComp<RMCTetherComponent>(uid);
        }
    }
}
