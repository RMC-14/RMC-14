using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Acid;

public sealed partial class XenoCorrosiveAcidEvent : EntityTargetActionEvent
{
    [DataField]
    public EntProtoId AcidId = "XenoAcidNormal";

    [DataField]
    public XenoAcidStrength Strength = XenoAcidStrength.Normal;

    [DataField]
    public FixedPoint2 PlasmaCost = 100;

    [DataField]
    public int EnergyCost = 0;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(225);

    [DataField]
    public float Dps = 8;

    [DataField]
    public float ExpendableLightDps = 2.5f;

    [DataField]
    public float ApplyTimeMultiplier = 1;

}
