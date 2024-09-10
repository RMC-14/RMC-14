using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Fruit;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoPlantFruitActionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public bool CheckFruitSelected;

    [DataField(required: true), AutoNetworkedField]
    public bool CheckWeeds;
}
