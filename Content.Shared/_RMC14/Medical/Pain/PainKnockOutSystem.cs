using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared._RMC14.StatusEffect;
using Content.Shared.Throwing;
using Robust.Shared.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Chat;
using Content.Shared._RMC14.Xenonids.Devour;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed class PainKnockOutSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedSuicideSystem _suicide = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string PainKnockOutKey = "PainKnockOut";

    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    public override void Initialize()
    {
        SubscribeLocalEvent<PainKnockOutComponent, RMCStatusEffectTimeEvent>(OnUpdate);
        SubscribeLocalEvent<PainKnockOutComponent, XenoDevouredEvent>(OnDevour);
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

    private void OnDevour(Entity<PainKnockOutComponent> ent, ref XenoDevouredEvent args)
    {
        if (HasComp<DamageableComponent>(ent))
            OxyKill(ent);
    }

    private void OxyKill(Entity<DamageableComponent> ent)
    {
        _suicide.ApplyLethalDamage(ent, AsphyxiationType);
    }
}
