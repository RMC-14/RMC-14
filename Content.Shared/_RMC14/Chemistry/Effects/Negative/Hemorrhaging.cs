using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Emote;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hemorrhaging : RMCChemicalEffect
{
    public override string Abbreviation => "HMR";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14
        return $"Removes [color=red]10[/color] units of blood with a {PotencyPerSecond * 5f:F2}% chance.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!IsHumanoid(args))
            return;

        // TODO RMC14 random limb, not robotic
        if (ProbHundred(potency.Float() * 5f))
        {
            // TODO RMC14 add internal bleeding
        }

        if (ProbHundred(potency.Float() * 5f))
        {
            var chat = System<SharedCMChatSystem>(args);
            chat.Emote(args.TargetEntity, "coughs up blood!");

            var bloodstream = System<SharedBloodstreamSystem>(args);
            bloodstream.TryModifyBloodLevel(args.TargetEntity, -10);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 organ damage
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 internal bleeding
    }
}
