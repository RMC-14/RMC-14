using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.CriticalGrace;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InCriticalGraceComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan GraceEndsAt;

    [DataField, AutoNetworkedField]
    public bool Over = false;
}
