using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class CMVendorMapToSquadComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId? Default;

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntProtoId> Map = new();
}
