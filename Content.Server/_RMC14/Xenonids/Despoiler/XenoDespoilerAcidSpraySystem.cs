using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Despoiler;
using Content.Shared.Damage;
using Content.Shared.Mobs.Components;
using Content.Shared.Stunnable;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Xenonids.Despoiler;

public sealed class XenoDespoilerAcidSpraySystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly XenoDespoilerAcidSystem _acid = default!;
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

    private void OnCollide(EntityUid uid, XenoDespoilerAcidSprayComponent comp, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;
        if (!_mobStateQuery.HasComp(target) || _xenoQuery.HasComp(target) || _immunityQuery.HasComp(target))
            return;

        _damageable.TryChangeDamage(target, comp.Damage, ignoreResistances: false, origin: comp.Caster);

        if (comp.Caster is { } caster)
            _acid.ApplyAcid(target, caster);

        if (comp.StunsOnEmpowered)
        {
            _stun.TryParalyze(target, comp.StunDuration, true);

            var immunity = EnsureComp<XenoDespoilerAcidImmunityComponent>(target);
            var expires = _timing.CurTime + comp.GrantImmunityDuration;
            if (expires > immunity.ExpiresAt)
                immunity.ExpiresAt = expires;
            Dirty(target, immunity);
        }
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
