﻿using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.StatusEffect;

public sealed class RMCStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly SkillsSystem _skills = default!;

    private static readonly EntProtoId<SkillDefinitionComponent> EnduranceSkill = "RMCSkillEndurance";
    private static readonly ProtoId<StatusEffectPrototype> Knockdown = "KnockedDown";
    private static readonly ProtoId<StatusEffectPrototype> Stun = "Stun";

    public override void Initialize()
    {
        SubscribeLocalEvent<SkillsComponent, RMCStatusEffectTimeEvent>(OnSkillsStatusEffectTime);
        SubscribeLocalEvent<XenoComponent, RMCStatusEffectTimeEvent>(OnXenoStatusEffectTime);
    }

    private void OnSkillsStatusEffectTime(Entity<SkillsComponent> ent, ref RMCStatusEffectTimeEvent args)
    {
        if (args.Key != Knockdown && args.Key != Stun)
            return;

        var endurance = _skills.GetSkill((ent, ent), EnduranceSkill);
        if (endurance < 1)
            return;

        var skill = (endurance - 1) * 0.08;
        var multiplier = 1 - skill; // TODO RMC14 hunter/synthetic/monkey/zombie/human hero multiplier
        args.Duration *= multiplier;
    }

    private void OnXenoStatusEffectTime(Entity<XenoComponent> ent, ref RMCStatusEffectTimeEvent args)
    {
        if (args.Key != Knockdown && args.Key != Stun)
            return;

        args.Duration *= 0.667;
    }
}
