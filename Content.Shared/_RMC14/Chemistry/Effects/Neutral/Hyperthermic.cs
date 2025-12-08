using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Temperature;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Temperature;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hyperthermic : RMCChemicalEffect
{
    private static readonly ProtoId<EmotePrototype> GaspEmote = "Gasp";

    public override string Abbreviation => "HPR";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 dizziness
        // TODO RMC14 agony
        return $"Raises body temperature by [color=red]{Potency * 2}ºC[/color], up to a maximum of 120ºC (248ºF).\n" +
               $"Overdoses raise body temperature by [color=red]{Potency * 5}ºC[/color], up to a maximum of 120ºC (248ºF).\n" +
               $"Critical overdoses paralyze for [color=red]2[/color] seconds.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (ProbHundred(10))
        {
            var emoteSystem = args.EntityManager.System<SharedRMCEmoteSystem>();
            emoteSystem.TryEmoteWithChat(
                args.TargetEntity,
                GaspEmote,
                hideLog: true,
                ignoreActionBlocker: true,
                forceEmote: true
            );

            var popup = System<SharedPopupSystem>(args);
            var net = IoCManager.Resolve<INetManager>();
            if (net.IsServer)
                popup.PopupClient("Your insides feel uncomfortably hot!", args.TargetEntity, args.TargetEntity);
        }

        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedRMCTemperatureSystem>();
        var current = sys.GetTemperature(args.TargetEntity);
        var change = (potency * 2 * args.Scale.Float()).Float();
        var temp = Math.Min(TemperatureHelpers.CelsiusToKelvin(120), current + change);

        sys.ForceChangeTemperature(args.TargetEntity, temp);

        // TODO RMC14 dizziness
        // TODO RMC14 agony
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedRMCTemperatureSystem>();
        var current = sys.GetTemperature(args.TargetEntity);
        var change = (potency * 5 * args.Scale.Float()).Float();
        var temp = Math.Min(TemperatureHelpers.CelsiusToKelvin(120), current + change);

        sys.ForceChangeTemperature(args.TargetEntity, temp);

        // TODO RMC14 dizziness
        // TODO RMC14 agony
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var stun = System<SharedStunSystem>(args);
        stun.TryParalyze(args.TargetEntity, TimeSpan.FromSeconds(2), true);
    }
}
