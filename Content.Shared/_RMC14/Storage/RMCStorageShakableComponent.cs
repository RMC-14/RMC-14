using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Storage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCStorageShakableComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags ShakableSlots = SlotFlags.BACK;

    [DataField, AutoNetworkedField]
    public bool ShakableInHand = true;

    [DataField, AutoNetworkedField]
    public float ShakeFailChance = 0.25f;

    //TODO RMC14 seperate comp for xenos to shake off storages
}
