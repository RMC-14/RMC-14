using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Acid;

[Serializable, NetSerializable]
public sealed partial class XenoCorrosiveAcidDoAfterEvent : DoAfterEvent
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
    public SoundSpecifier AcidSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/acid_impact1.ogg", AudioParams.Default.WithVolume(-6f));

    public XenoCorrosiveAcidDoAfterEvent(XenoCorrosiveAcidEvent ev)
    {
        AcidId = ev.AcidId;
        Strength = ev.Strength;
        PlasmaCost = ev.PlasmaCost;
        Time = ev.Time;
        Dps = ev.Dps;
        ExpendableLightDps = ev.ExpendableLightDps;
        EnergyCost = ev.EnergyCost;
        AcidSound = ev.AcidSound;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
