using Content.Shared._RMC14.Marines.Squads;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Pointing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCPointingSystem), typeof(SquadSystem))]
public sealed partial class RMCPointingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Arrow = "RMCPointingArrowBig";

    [DataField, AutoNetworkedField]
    public EntProtoId SquadArrow = "RMCPointingArrowSquad";

    [DataField, AutoNetworkedField]
    public bool Intrinsic = true;
}
