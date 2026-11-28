using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GlassesAccessoriesSystem))]
public sealed partial class GlassesAccessoryHolderComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slot = SlotFlags.EYES;

    [DataField, AutoNetworkedField]
    public bool IsHat = true;
}

public enum GlassesAccessoryLayers
{
    Glasses
}
