using Content.Shared._RMC14.Emote;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Allergenic : RMCChemicalEffect
{
    private static readonly ProtoId<EmotePrototype>[] Emotes =
    [
        "Sneeze",
        "RMCBlink",
        "Cough",
    ];

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Creates a hyperactive immune response in the body, resulting in irritation.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f * (float) potency))
            return;

        var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
        emote.TryEmoteWithChat(args.TargetEntity, random.Pick(Emotes), hideLog: true, forceEmote: true);
    }
}
