using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Tail_Lash;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoTailLashComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 Cost = 80;

    [DataField, AutoNetworkedField]
    public TimeSpan Windup = TimeSpan.FromSeconds(0.2);

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(13);

    [DataField, AutoNetworkedField]
    public float Width = 3;

    [DataField, AutoNetworkedField]
    public float Height = 2;

    [DataField, AutoNetworkedField]
    public float FlingDistance = 3;

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(2.5);

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "RMCEffectXenoTelegraphLash";

    [DataField, AutoNetworkedField]
    public EntProtoId EffectEdge = "RMCEffectXenoTelegraphLashAnim";

    [DataField, AutoNetworkedField]
    public Box2Rotated? Area;
}
