using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Medical.Surgery;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMSurgerySystem))]
[EntityCategory("Surgeries")]
public sealed partial class CMSurgeryComponent : Component
{
    [DataField, AutoNetworkedField, Access(typeof(SharedCMSurgerySystem), Other = AccessPermissions.ReadWriteExecute)]
    public int Priority;

    [DataField, AutoNetworkedField]
    public EntProtoId? Requirement;

    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> Steps = new();
}
