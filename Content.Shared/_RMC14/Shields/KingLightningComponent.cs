using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Shields;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KingLightningComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Source;

    [DataField]
    public EntProtoId Lightning = "RMCPurpleLightning";

    [DataField]
    public TimeSpan DisappearAt;

    [DataField]
    public List<EntityUid> Trail = new();

    [DataField]
    public bool StopUpdating;
}
