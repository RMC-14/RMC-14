using Content.Shared._RMC14.Slow;
using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Heal;

public sealed partial class XenoSacrificeHealActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float Range = 2.0F;

    /// <summary>
    /// What proportion of current health will be sent to target
    /// </summary>
    [DataField]
    public FixedPoint2 TransferProportion = 0.75;

    /// <summary>
    /// Assuming respawn, how long after death will respawn occur
    /// </summary>
    [DataField]
    public TimeSpan RespawnDelay = TimeSpan.FromSeconds(0.5);

    [DataField]
    public EntProtoId HealEffect = "RMCEffectHealSacrifice";

    [DataField]
    public ProtoId<StatusEffectPrototype>[] AilmentsRemove = ["KnockedDown", "Stun", "Dazed", "Unconscious"];

    [DataField]
    public ComponentRegistry ComponentsRemove;
}
