using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Chemistry.Effects;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Damage;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Medical;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._RMC14.Medical.Defibrillator;

public sealed class RMCDefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

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

        if (!_rmcBloodstream.TryGetChemicalSolution(args.Target, out var solutionEnt, out _))
            return;

        (Reagent Reagent, FixedPoint2 Heal, Electrogenetic Electrogenetic)? highest = null;
        foreach (var quantity in solutionEnt.Comp.Solution.Contents)
        {
            if (!_rmcReagent.TryIndex(quantity.Reagent.Prototype, out var reagent))
                continue;

            if (reagent.Metabolisms == null ||
                !reagent.Metabolisms.TryGetValue(ent.Comp.MetabolismId, out var effects))
            {
                continue;
            }

            foreach (var effect in effects.Effects)
            {
                if (effect is not Electrogenetic electrogenetic)
                    continue;

                if (highest == null || electrogenetic.HealAmount > highest.Value.Heal)
                    highest = (reagent, electrogenetic.HealAmount, electrogenetic);
            }
        }

        if (highest == null)
            return;

        args.Heal += highest.Value.Electrogenetic.CalculateHeal(_damageable, args.Target, EntityManager);
        _solutionContainer.RemoveReagent(solutionEnt, highest.Value.Reagent.ID, 1);
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
