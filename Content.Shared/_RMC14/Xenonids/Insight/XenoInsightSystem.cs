using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Xenonids.DeployTraps;
using Content.Shared._RMC14.Xenonids.Insight;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Shotgun;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.Insight;

public sealed class XenoInsightSystem : EntitySystem
{
    [Dependency] private readonly SharedProjectileSystem _projectileSystem = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<ProjectileComponent> _projectileQuery = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public int GetInsight(EntityUid uid)
    {
        if (!TryComp<XenoInsightComponent>(uid, out var insight))
            return 0;

        return insight.Insight;
    }

    public void IncrementInsight(Entity<XenoInsightComponent?> xeno, int amount)
    {
        if (!Resolve(xeno, ref xeno.Comp, false))
            return;

        xeno.Comp.Insight += amount;
        xeno.Comp.Insight = Math.Min(xeno.Comp.Insight, xeno.Comp.MaxInsight);
        Dirty(xeno);

        if (xeno.Comp.Insight >= xeno.Comp.MaxInsight)
            InsightEmpower((xeno.Owner, xeno.Comp));
    }

    public void InsightEmpower(Entity<XenoInsightComponent> xeno)
    {
        if (TryComp(xeno.Owner, out XenoDeployTrapsComponent? deployTraps))
            deployTraps.Empowered = true;
        _popup.PopupClient(Loc.GetString("rmc-xeno-insight-empower"), xeno, xeno, PopupType.Medium);
    }
}

