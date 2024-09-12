using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Fruit.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoPlantFruitActionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public bool CheckFruitSelected;

    [DataField(required: true), AutoNetworkedField]
    public bool CheckWeeds;

    [DataField, AutoNetworkedField]
    public TimeSpan PlantCooldown = TimeSpan.FromSeconds(5);
}

