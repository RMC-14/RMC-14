using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Communications;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CommunicationsTowerSystem))]
public sealed partial class CommunicationsTowerSpawnerComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Group;

    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCCommunicationsTower";
}
