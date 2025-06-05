using Content.Shared._RMC14.Marines.Skills;
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
    private static readonly ProtoId<StatusEffectPrototype> Unconscious = "Unconscious";
    private static readonly ProtoId<StatusEffectPrototype> Dazed = "Dazed";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SkillsComponent, RMCStatusEffectTimeEvent>(OnSkillsStatusEffectTime);
        SubscribeLocalEvent<XenoComponent, RMCStatusEffectTimeEvent>(OnXenoStatusEffectTime);
        SubscribeLocalEvent<RMCStunResistanceComponent, RMCStatusEffectTimeEvent>(OnStunResistanceStatusEffectTime);
    }

    private void OnSkillsStatusEffectTime(Entity<SkillsComponent> ent, ref RMCStatusEffectTimeEvent args)
    {
        if (args.Key != Knockdown && args.Key != Stun && args.Key != Unconscious && args.Key != Dazed)
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
        if (args.Key != Knockdown && args.Key != Stun && args.Key != Unconscious && args.Key != Dazed)
            return;

        args.Duration *= 0.667;
    }

    private void OnStunResistanceStatusEffectTime(Entity<RMCStunResistanceComponent> ent, ref RMCStatusEffectTimeEvent args)
    {
        if (args.Key != Knockdown && args.Key != Stun && args.Key != Unconscious && args.Key != Dazed)
            return;

        args.Duration /= ent.Comp.Resistance;
    }

    public void GiveStunResistance(EntityUid target, float resistance)
    {
        var resistanceComp = EnsureComp<RMCStunResistanceComponent>(target);
        resistanceComp.Resistance = resistance;
        Dirty(target, resistanceComp);
    }
}
