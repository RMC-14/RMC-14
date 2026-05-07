using Content.Shared._RMC14.BlurredVision;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chat.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class RMCPepperSpray : EntityEffect
{
    private static readonly EntProtoId<SkillDefinitionComponent> PoliceSkill = "RMCSkillPolice";
    private static readonly ProtoId<EmotePrototype> Scream = "Scream";

    [DataField]
    public TimeSpan ProtectedBlurTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan BlurTime = TimeSpan.FromSeconds(25);

    [DataField]
    public TimeSpan BlindTime = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(3);

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var target = args.TargetEntity;
        var entMan = args.EntityManager;

        if (!entMan.HasComponent<HumanoidAppearanceComponent>(target))
            return;

        var mobState = entMan.System<MobStateSystem>();
        if (mobState.IsDead(target))
            return;

        var skills = entMan.System<SkillsSystem>();
        var status = entMan.System<StatusEffectsSystem>();
        var popup = entMan.System<SharedPopupSystem>();

        if (skills.HasSkill(target, PoliceSkill, 2))
        {
            status.TryAddStatusEffect<RMCBlindedComponent>(target, "Blinded", ProtectedBlurTime, true);
            popup.PopupEntity(Loc.GetString("rmc-pepper-spray-protected"), target, target, PopupType.MediumCaution);
            return;
        }

        entMan.System<SharedRMCEmoteSystem>().TryEmoteWithChat(
            target,
            Scream,
            hideLog: true,
            ignoreActionBlocker: true,
            forceEmote: true
        );

        popup.PopupEntity(Loc.GetString("rmc-pepper-spray-hit"), target, target, PopupType.LargeCaution);
        status.TryAddStatusEffect<RMCBlindedComponent>(target, "Blinded", BlurTime, true);
        status.TryAddStatusEffect<TemporaryBlindnessComponent>(target, "TemporaryBlindness", BlindTime, true);

        var stun = entMan.System<SharedStunSystem>();
        stun.TryStun(target, StunTime, true);
        stun.TryKnockdown(target, KnockdownTime, true);
    }
}
