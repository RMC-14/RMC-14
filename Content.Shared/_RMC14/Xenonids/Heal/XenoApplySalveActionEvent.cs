using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Heal;

public sealed partial class XenoApplySalveActionEvent : EntityTargetActionEvent
{
    [DataField]
    public float Range = 2.0F;

    /// <summary>
    /// Plasma cost is [PlasmaCostModifier x heal amount]
    /// </summary>
    [DataField]
    public float PlasmaCostModifier = 2.0F;

    /// <summary>
    /// The amount of damage this xeno take is [DamageTakenModifier x heal amount]
    /// </summary>
    [DataField]
    public FixedPoint2 DamageTakenModifier = 0.75F;

    [DataField]
    public FixedPoint2 StandardHealAmount = 100F;

    [DataField]
    public FixedPoint2 SmallHealAmount = 15F;

    [DataField]
    public TimeSpan TimeBetweenHeals = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan TotalHealDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public EntProtoId HealEffect = "RMCEffectHealHealer";

    [DataField]
    public SoundSpecifier HealSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_drool1.ogg");
}
