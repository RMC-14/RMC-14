using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Dissolver;

public sealed partial class XenoDissolverBombardActionEvent : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId BaseSpit = "XenoChargedSpitProjectileDissolverCorrosive";

    [DataField]
    public int ExtraProjectiles = 0;

    [DataField]
    public Angle Scatter = 0;

    [DataField]
    public TimeSpan Windup = TimeSpan.FromSeconds(0.6);

    [DataField]
    public TimeSpan WindupReductionPerStack = TimeSpan.Zero;

    //Must be either 0 or have elements equal to max charges
    [DataField]
    public EntProtoId[] SpitTypePerChargeLevel = [];

    [DataField]
    public int Speed = 30;

    [DataField]
    public int PlasmaCost = 45;

    [DataField]
    public float? MaxDistance;
}
