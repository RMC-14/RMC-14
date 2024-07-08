using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Lifesteal;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoLifestealSystem))]
public sealed partial class XenoLifestealComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 BasePercentage = 0.07;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxPercentage = 0.09;

    [DataField, AutoNetworkedField]
    public FixedPoint2 TargetIncreasePercentage = 0.01;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MinHeal = 20;

    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxHeal = 40;

    [DataField, AutoNetworkedField]
    public float TargetRange = 3;
}
