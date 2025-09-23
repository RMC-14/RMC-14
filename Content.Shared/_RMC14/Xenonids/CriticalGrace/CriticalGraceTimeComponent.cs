using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.CriticalGrace;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CriticalGraceTimeComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan GraceDuration = TimeSpan.Zero; //TODO RMC14 1 second
}
