using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(HelmetAccessoriesSystem))]
public sealed partial class HelmetAccessoryHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slot = SlotFlags.HEAD;

    [DataField, AutoNetworkedField]
    public bool IsHat = true;
}

public enum HelmetAccessoryLayers
{
    Helmet
}
