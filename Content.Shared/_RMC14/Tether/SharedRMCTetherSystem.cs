using Content.Shared._RMC14.Projectiles;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Tether;

public abstract partial class SharedRMCTetherSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;

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
}
