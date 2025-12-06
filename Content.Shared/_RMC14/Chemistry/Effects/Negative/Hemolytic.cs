using System.Collections.Immutable;
using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Drowsyness;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Stun;
using Content.Shared.Body.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Content.Shared.StatusEffect;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hemolytic : RMCChemicalEffect
{
    private static readonly ProtoId<StatusEffectPrototype> Unconscious = "Unconscious";

    private static readonly ProtoId<EmotePrototype> GaspEmote = "Gasp";
    private static readonly ProtoId<EmotePrototype> YawnEmote = "Yawn";

    private static readonly ImmutableArray<ProtoId<EmotePrototype>> Emotes = ImmutableArray.Create(GaspEmote, YawnEmote);

    public override string Abbreviation => "HML";

    [DataField]
    public FixedPoint2 SpeedMultiplier = 0.15;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Removes [color=red]{PotencyPerSecond * 5}[/color] units of blood.\n" +
               $"Overdoses remove [color=red]{PotencyPerSecond * 8}[/color] units of blood, add [color=red]{PotencyPerSecond}[/color] drowsyness, and reduces speed by [color=red]{PotencyPerSecond}[/color]%.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] asphyxiation damage.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = System<SharedBloodstreamSystem>(args);
        bloodstream.TryModifyBloodLevel(args.TargetEntity, -potency * 5);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var bloodstream = System<SharedBloodstreamSystem>(args);
        bloodstream.TryModifyBloodLevel(args.TargetEntity, -potency * 8);

        if (TryComp(args, out DrowsynessComponent? drowsyness))
        {
            var drowsynessSys = System<DrowsynessSystem>(args);
            drowsynessSys.TryChange(args.TargetEntity, FixedPoint2.Min(15 * potency, drowsyness.Amount + potency));
        }

        var speed = EnsureComp<HemolyticSpeedComponent>(args);
        speed.Multiplier = FixedPoint2.Max(SpeedMultiplier, 1 - potency.Float() * SpeedMultiplier).Float();
        speed.AppliedAt = IoCManager.Resolve<IGameTiming>().CurTime;

        if (!ProbHundred(10))
            return;

        var emoteSystem = args.EntityManager.System<SharedRMCEmoteSystem>();
        var emote = IoCManager.Resolve<IRobustRandom>().Pick(Emotes);
        emoteSystem.TryEmoteWithChat(
            args.TargetEntity,
            emote,
            hideLog: true,
            ignoreActionBlocker: true,
            forceEmote: true
        );
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, AsphyxiationType, potency * 5);
    }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(HemolyticSystem), typeof(Hemolytic))]
public sealed partial class HemolyticSpeedComponent : ReagentSpeedModifierComponent
{
    [DataField, AutoNetworkedField]
    public override float Multiplier { get; set; }

    [DataField, AutoNetworkedField, AutoPausedField]
    public override TimeSpan AppliedAt { get; set; }
}

public sealed class HemolyticSystem : ReagentSpeedModifierSystem<HemolyticSpeedComponent>;
