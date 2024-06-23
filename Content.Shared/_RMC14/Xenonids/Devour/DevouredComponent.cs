using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Devour;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoDevourSystem))]
public sealed partial class DevouredComponent : Component
{
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan WarnAt;

    [DataField, AutoNetworkedField]
    public bool Warned;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan RegurgitateAt;
}
