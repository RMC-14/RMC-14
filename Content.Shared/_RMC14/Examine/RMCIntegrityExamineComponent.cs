using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Localization;

namespace Content.Shared._RMC14.Examine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCIntegrityExamineComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId PercentMessage = "rmc-wall-integrity-percent";

    [DataField, AutoNetworkedField]
    public FixedPoint2? MaxIntegrity;
}
