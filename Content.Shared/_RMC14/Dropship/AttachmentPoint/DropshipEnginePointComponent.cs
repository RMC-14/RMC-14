using Content.Shared._RMC14.Dropship.Utility.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship.AttachmentPoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipSystem), typeof(DropshipUtilitySystem))]
public sealed partial class DropshipEnginePointComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_dropship_engine_point_container";
}
