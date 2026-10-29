using System.Diagnostics.CodeAnalysis;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Chemistry.Effects;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Synth;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffect;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.Defibrillator;

public abstract class SharedRMCDefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _rmcEmote = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<EmotePrototype> GaspEmote = "Gasp";
    private static readonly TimeSpan ReviveKnockOut = TimeSpan.FromSeconds(20);
    private static readonly ProtoId<StatusEffectPrototype> BlindedStatus = "Blinded";
    private static readonly TimeSpan ReviveBlur = TimeSpan.FromSeconds(20);

    public override void Initialize()
    {
        SubscribeLocalEvent<DefibrillatorComponent, RMCDefibrillatorDamageModifyEvent>(OnDefibrillatorDamageModify);
        SubscribeLocalEvent<RMCDefibrillatorAudioComponent, EntityTerminatingEvent>(OnDefibrillatorAudioTerminating);
        SubscribeLocalEvent<RMCDefibrillatorBlockedComponent, ExaminedEvent>(OnNoDefibExamine);
    }

    private void OnDefibrillatorDamageModify(Entity<DefibrillatorComponent> ent, ref RMCDefibrillatorDamageModifyEvent args)
    {
        if (ent.Comp.RMCZapDamage != null)
        {
            foreach (var (group, amount) in ent.Comp.RMCZapDamage)
            {
                args.Heal = _rmcDamageable.DistributeDamageCached(args.Target, group, amount, args.Heal);
            }
        }

        if (TryConsumeElectrogenetic(args.Target, out var electrogeneticHeal))
            args.Heal += electrogeneticHeal;
    }

    public bool TryConsumeElectrogenetic(EntityUid target, [NotNullWhen(true)] out DamageSpecifier? heal)
    {
        heal = null;
        if (HasComp<SynthComponent>(target))
            return false;

        if (!_rmcBloodstream.TryGetChemicalSolution(target, out var solutionEnt, out var solution))
            return false;

        foreach (var quantity in solution.Contents)
        {
            if (!_rmcReagent.TryGetProperty<Electrogenetic>(quantity.Reagent.Prototype, out var electrogenetic))
                continue;

            heal = electrogenetic.CalculateHeal(_damageable, target, EntityManager);
            _solutionContainer.RemoveReagent(solutionEnt, quantity.Reagent.Prototype, 1);
            return true;
        }

        return false;
    }

    public void ApplyReviveEffects(EntityUid target)
    {
        _rmcEmote.TryEmoteWithChat(target, GaspEmote, hideLog: true, ignoreActionBlocker: true, forceEmote: true);
        _sizeStun.TryKnockOut(target, ReviveKnockOut);

        if (!_statusEffects.TryGetTime(target, BlindedStatus, out var blur) || blur.Value.Item2 - _timing.CurTime < ReviveBlur)
            _statusEffects.TryAddStatusEffect<RMCBlindedComponent>(target, BlindedStatus, ReviveBlur, true);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCDefibrillatingReviveComponent, MobStateComponent, DamageableComponent>();
        while (query.MoveNext(out var uid, out var revive, out var mobState, out var damageable))
        {
            if (!_mobState.IsDead(uid, mobState) ||
                !_mobThreshold.TryGetThresholdForState(uid, MobState.Dead, out var deadThreshold) ||
                damageable.TotalDamage >= deadThreshold)
            {
                RemCompDeferred<RMCDefibrillatingReviveComponent>(uid);
                continue;
            }

            if (time < revive.RevivesAt)
                continue;

            RemCompDeferred<RMCDefibrillatingReviveComponent>(uid);
            _mobState.ChangeMobState(uid, MobState.Critical, mobState);
            ApplyReviveEffects(uid);
        }
    }

    private void OnNoDefibExamine(Entity<RMCDefibrillatorBlockedComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.ShowOnExamine)
            return;

        args.PushMarkup(Loc.GetString(ent.Comp.Examine, ("victim", ent)));
    }

    private void OnDefibrillatorAudioTerminating(Entity<RMCDefibrillatorAudioComponent> ent, ref EntityTerminatingEvent args)
    {
        if (TryComp(ent.Comp.Defibrillator, out DefibrillatorComponent? defibrillator))
            defibrillator.ChargeSoundEntity = null;
    }

    public void StopChargingAudio(Entity<DefibrillatorComponent> defib)
    {
        _audio.Stop(defib.Comp.ChargeSoundEntity);
        QueueDel(defib.Comp.ChargeSoundEntity);
        defib.Comp.ChargeSoundEntity = null;
    }
}

[Serializable, NetSerializable]
public enum DefibrillatorChargeVisuals
{
    Empty,
    Low,
    Half,
    Full
}

[Serializable, NetSerializable]
public enum DefibrillatorVisuals
{
    DefibrillatorCharge
}

[Serializable, NetSerializable]
public enum DefibrillatorVisualLayers
{
    Base,
    Overlay
}
