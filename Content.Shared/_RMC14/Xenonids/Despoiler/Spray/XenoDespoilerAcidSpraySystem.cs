using Content.Shared._RMC14.Xenonids.Despoiler.Acid;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Despoiler.Spray;

public sealed class XenoDespoilerAcidSpraySystem : EntitySystem
{
    [Dependency] private readonly XenoDespoilerAcidSystem _acid = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<XenoComponent> _xenoQuery;
    private EntityQuery<XenoDespoilerAcidImmunityComponent> _immunityQuery;

    public override void Initialize()
    {
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _xenoQuery = GetEntityQuery<XenoComponent>();
        _immunityQuery = GetEntityQuery<XenoDespoilerAcidImmunityComponent>();

        SubscribeLocalEvent<XenoDespoilerAcidSprayComponent, StartCollideEvent>(OnCollide);
    }

    private void OnCollide(Entity<XenoDespoilerAcidSprayComponent> ent, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        if (!_mobStateQuery.HasComp(target) || _xenoQuery.HasComp(target) || _immunityQuery.HasComp(target))
            return;

        _damageable.TryChangeDamage(target, ent.Comp.Damage, ignoreResistances: false, origin: ent.Comp.Caster);

        if (ent.Comp.Caster is { } caster)
            _acid.ApplyAcid(target, caster);

        if (!ent.Comp.StunsOnEmpowered)
            return;

        _stun.TryParalyze(target, ent.Comp.StunDuration, true);

        var immunity = EnsureComp<XenoDespoilerAcidImmunityComponent>(target);
        var expires = _timing.CurTime + ent.Comp.GrantImmunityDuration;
        if (expires <= immunity.ExpiresAt)
            return;

        immunity.ExpiresAt = expires;
        Dirty(target, immunity);
    }

    public override void Update(float frameTime)
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<XenoDespoilerAcidImmunityComponent>();
        while (query.MoveNext(out var uid, out var immunity))
        {
            if (now >= immunity.ExpiresAt)
                RemCompDeferred<XenoDespoilerAcidImmunityComponent>(uid);
        }
    }
}
