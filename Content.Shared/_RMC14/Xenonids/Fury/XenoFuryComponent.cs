using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Fury;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoFuryComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Heal = 15;

    [DataField, AutoNetworkedField]
    public int BoostedHeal = 25;

    [DataField, AutoNetworkedField]
    public EntProtoId Effect = "RMCEffectHeal";

    [DataField, AutoNetworkedField]
    public float Range = 3;
}
