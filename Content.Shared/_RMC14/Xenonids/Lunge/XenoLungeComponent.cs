using System.Numerics;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Lunge;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoLungeSystem))]
public sealed partial class XenoLungeComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 5;

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "CMEffectGrab";

    [DataField, AutoNetworkedField]
    public Vector2? Charge;
}
