using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared._RMC14.StatusEffect;

namespace Content.Shared._RMC14.Medical.Pain;

public sealed class PainKnockOutSystem : EntitySystem
{
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    [ValidatePrototypeId<StatusEffectPrototype>]
    private const string PainKnockOutKey = "PainKnockOut";
    public override void Initialize()
    {
        SubscribeLocalEvent<PainKnockOutComponent, RMCStatusEffectTimeEvent>(OnUpdate);
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
}
