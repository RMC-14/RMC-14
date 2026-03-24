using Content.Shared.Projectiles;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Debilitate;

public sealed class RMCDebilitateSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCDebilitateComponent, ProjectileHitEvent>(OnDebilitateProjectileHit);
    }

    private void OnDebilitateProjectileHit(Entity<RMCDebilitateComponent> ent, ref ProjectileHitEvent args)
    {
        if (!_entityWhitelist.CheckBoth(args.Target, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        _stun.TryParalyze(args.Target, ent.Comp.Knockdown, true);
    }
}
