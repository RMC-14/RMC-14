using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared._RMC14.StatusEffect;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Chat;
using Content.Shared._RMC14.Xenonids.Devour;
using Content.Shared.Pulling.Events;
using Content.Shared._RMC14.Xenonids;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed class PainKnockOutSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string PainKnockOutKey = "PainKnockOut";

    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    public override void Initialize()
    {
        SubscribeLocalEvent<PainKnockOutComponent, RMCStatusEffectTimeEvent>(OnUpdate);
        SubscribeLocalEvent<PainKnockOutComponent, XenoDevouredEvent>(OnDevour);
        SubscribeLocalEvent<PainKnockOutComponent, BeingPulledAttemptEvent>(OnPull);
    }

    private void OnUpdate(EntityUid uid, PainKnockOutComponent component, RMCStatusEffectTimeEvent args)
    {
        if (args.Key != PainKnockOutKey)
            return;

        var time = args.Duration;

        _stun.TryParalyze(uid, time, true);
        _status.TryAddStatusEffect(uid, "Muted", time, true, "Muted");
        _status.TryAddStatusEffect(uid, "TemporaryBlindness", time, true, "TemporaryBlindness");
    }

    // TODO throw damage? https://github.com/cmss13-devs/cmss13/blob/3cb6156d41f212948c65488deb24166e5115e75d/code/datums/pain/_pain.dm#L199

    private void OnPull(Entity<PainKnockOutComponent> ent, ref BeingPulledAttemptEvent args)
    {
        if (HasComp<DamageableComponent>(args.Pulled) && HasComp<XenoComponent>(args.Puller))
        {
            var pullAsphyxation = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>(AsphyxiationType), 20);
            _damageable.TryChangeDamage(args.Pulled, pullAsphyxation, true);
        }
    }

    private void OnDevour(Entity<PainKnockOutComponent> ent, ref XenoDevouredEvent args)
    {
        if (TryComp<DamageableComponent>(args.Target, out var damageable))
        {
            var devoured = new Entity<DamageableComponent>(args.Target, damageable);
            OxyKill(devoured);
        }
    }
    private void OxyKill(Entity<DamageableComponent> ent)
    {
        _suicide.ApplyLethalDamage(ent, AsphyxiationType);
    }
}
