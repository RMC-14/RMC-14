using System.Collections.Immutable;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Euphoric : RMCChemicalEffect
{
    private static readonly ImmutableArray<string> Emotes = ImmutableArray.Create("laugh", "giggle", "chuckle", "grin", "smile", "twitch");

    public override string Abbreviation => "EPH";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return //$"Reduces pain by .\n" +
               $"Overdoses cause a [color=red]{PotencyPerSecond * 5}%[/color] to collapse on the ground.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] oxygen damage";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 Pain Reduction

        if (ProbHundred(5 * potency))
        {
            var emote = IoCManager.Resolve<IRobustRandom>().Pick(Emotes);
            var chat = System<SharedCMChatSystem>(args);
            chat.Emote(args.TargetEntity, emote);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (ProbHundred(5 * potency))
        {
            var knockOut = System<RMCSizeStunSystem>(args);
            knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(4), true);

            var chat = System<SharedCMChatSystem>(args);
            chat.Emote(args.TargetEntity, "collapses!");
        }
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, AsphyxiationType, 5 * potency);

        var popup = System<SharedPopupSystem>(args);
        var net = IoCManager.Resolve<INetManager>();
        if (net.IsServer)
            popup.PopupClient("You are laughing so much you can't breathe!", args.TargetEntity, args.TargetEntity);
    }
}
