using Content.Shared._RMC14.Dizziness;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Atmos;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Temperature;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hyperthermic : RMCChemicalEffect
{
    private static readonly float MaxTemperature = TemperatureHelpers.CelsiusToKelvin(120f);
    private static readonly ProtoId<EmotePrototype> GaspEmote = "Gasp";

    public override bool IsVolatile => true;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes an exothermic reaction when metabolized, increasing internal body temperature.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
        {
            var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
            emote.TryEmoteWithChat(args.TargetEntity, GaspEmote, hideLog: true, forceEmote: true);
            args.EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-hyperthermic-hot"), args.TargetEntity, args.TargetEntity, PopupType.MediumCaution);
        }

        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        var newTemp = MathF.Min(MaxTemperature, temperature.GetTemperature(args.TargetEntity) + (float) potency * 2f);
        temperature.ForceChangeTemperature(args.TargetEntity, newTemp);

        if (ActualPotency >= 2)
        {
            args.EntityManager.System<RMCDizzinessSystem>().MakeDizzy(args.TargetEntity, ActualPotency);

            // TODO RMC14: pain (agony) - +potency halloss per 2s tick at potency 2+
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        var newTemp = MathF.Min(MaxTemperature, temperature.GetTemperature(args.TargetEntity) + (float) potency * 5f);
        temperature.ForceChangeTemperature(args.TargetEntity, newTemp);

        // TODO RMC14: pain (agony) on overdose - +2 * potency halloss per 2s tick
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(40));
    }
}
