using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Dissolver;

[Serializable, NetSerializable]
public sealed partial class XenoDissolverBombardDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public EntProtoId SpitId;

    [DataField]
    public int ExtraProjectiles;

    [DataField]
    public Angle Scatter;

    [DataField]
    public int Speed;

    [DataField]
    public int PlasmaCost;

    [DataField]
    public float? MaxDistance;

    public XenoDissolverBombardDoAfterEvent(EntProtoId spitId, int extraProjectiles, Angle scatter, int speed, int plasmaCost, float? maxDistance)
    {
        SpitId = spitId;
        ExtraProjectiles = extraProjectiles;
        Scatter = scatter;
        Speed = speed;
        PlasmaCost = plasmaCost;
        MaxDistance = maxDistance;
    }
}
