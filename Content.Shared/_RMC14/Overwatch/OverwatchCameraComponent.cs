using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Overwatch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedOverwatchConsoleSystem))]
public sealed partial class OverwatchCameraComponent : Component, IClothingSlots
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.HEAD | SlotFlags.EARS;

    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Watching = new();
}
