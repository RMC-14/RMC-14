using Content.Shared._RMC14.Chat;
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
    private static readonly ProtoId<EmotePrototype> SneezeEmote = "Sneeze";
    private static readonly ProtoId<EmotePrototype> CoughEmote = "Cough";

    public override string Abbreviation => "ALG";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Creates a hyperactive immune response in the body, resulting in irritation";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (ProbHundred(5 * potency))
        {
            var random = IoCManager.Resolve<IRobustRandom>();
            var rmcEmote = System<SharedRMCEmoteSystem>(args);
            var cmChat = System<SharedCMChatSystem>(args);
            switch (random.Next(3))
            {
                case 0:
                    rmcEmote.TryEmoteWithChat(args.TargetEntity, SneezeEmote);
                    break;
                case 1:
                    rmcEmote.TryEmoteWithChat(args.TargetEntity, CoughEmote);
                    break;
                case 2:
                    cmChat.Emote(args.TargetEntity, "blinks");
                    break;
            }
        }
    }
}
