using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Stun;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Antispasmodic : RMCChemicalEffect
{
    private static readonly ProtoId<EmotePrototype> YawnEmote = "Yawn";

    public override string Abbreviation => "ASP";

    [DataField]
    public FixedPoint2 SpeedMultiplier = 0.15;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Slows down movement speed by [color=red]{PotencyPerSecond * SpeedMultiplier:F0}%[/color].\n" +
               $"Overdoses slow down movement speed by [color=red]{PotencyPerSecond * SpeedMultiplier * 2:F0}%[/color].\n" +
               $"Critical overdoses paralyze and cause [color=red]{PotencyPerSecond}[/color] asphyxiation damage.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var speed = EnsureComp<AntispasmodicSpeedComponent>(args);
        speed.Multiplier = FixedPoint2.Max(SpeedMultiplier, 1 - potency.Float() * SpeedMultiplier).Float();
        speed.AppliedAt = IoCManager.Resolve<IGameTiming>().CurTime;

        if (ProbHundred(5))
        {
            var emoteSystem = args.EntityManager.System<SharedRMCEmoteSystem>();
            emoteSystem.TryEmoteWithChat(
                args.TargetEntity,
                YawnEmote,
                hideLog: true,
                ignoreActionBlocker: true,
                forceEmote: true
            );
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var speed = EnsureComp<AntispasmodicSpeedComponent>(args);
        speed.Multiplier = FixedPoint2.Max(SpeedMultiplier, 1 - potency.Float() * SpeedMultiplier * 2).Float();
        speed.AppliedAt = IoCManager.Resolve<IGameTiming>().CurTime;

        var popup = System<SharedPopupSystem>(args);
        var net = IoCManager.Resolve<INetManager>();
        if (net.IsServer && ProbHundred(10))
            popup.PopupClient("You feel incredibly weak!", args.TargetEntity, args.TargetEntity);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var knockOut = System<RMCSizeStunSystem>(args);
        if (ProbHundred(7.5 * potency))
            knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(potency.Float() * 2f), true);

        TryChangeDamage(args, AsphyxiationType, potency);

        var popup = System<SharedPopupSystem>(args);
        var net = IoCManager.Resolve<INetManager>();
        if (net.IsServer && ProbHundred(2.5))
            popup.PopupClient("You can hardly breathe!", args.TargetEntity, args.TargetEntity);

        // TODO RMC14 organ damage heart
    }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(AntispasmodicSystem), typeof(Antispasmodic))]
public sealed partial class AntispasmodicSpeedComponent : ReagentSpeedModifierComponent
{
    [DataField, AutoNetworkedField]
    public override float Multiplier { get; set; }

    [DataField, AutoNetworkedField, AutoPausedField]
    public override TimeSpan AppliedAt { get; set; }
}

public sealed class AntispasmodicSystem : ReagentSpeedModifierSystem<AntispasmodicSpeedComponent>;
