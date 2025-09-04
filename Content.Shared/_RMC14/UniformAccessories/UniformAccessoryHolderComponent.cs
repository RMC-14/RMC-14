using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.UniformAccessories;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedUniformAccessorySystem))]
public sealed partial class UniformAccessoryHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ContainerId = "rmc_uniform_accessories";

    [DataField, AutoNetworkedField]
    public List<string> AllowedCategories;

    [DataField, AutoNetworkedField]
    public List<EntProtoId>? StartingAccessories;

    [DataField, AutoNetworkedField]
    public bool HideAccessories = false;
}
