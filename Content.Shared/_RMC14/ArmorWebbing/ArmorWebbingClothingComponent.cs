using Content.Shared.Item;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.ArmorWebbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedArmorWebbingSystem))]
public sealed partial class ArmorWebbingClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Container = "rmc_clothing_armor_webbing_slot";

    [DataField, AutoNetworkedField]
    public EntityUid? ArmorWebbing;

    [DataField, AutoNetworkedField]
    public EntProtoId<ArmorWebbingComponent>? StartingArmorWebbing;
}
