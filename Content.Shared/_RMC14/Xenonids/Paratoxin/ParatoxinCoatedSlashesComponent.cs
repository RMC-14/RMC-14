using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Paratoxin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParatoxinCoatedSlashesComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? NumberOfSlashes;

    [DataField, AutoNetworkedField]
    public int StacksPerSlash;

    [DataField, AutoNetworkedField]
    public TimeSpan ExpiresAt;
}
