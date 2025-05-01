using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadLeaderTrackerSystem))]
public sealed partial class GrantSquadLeaderTrackerComponent : Component, IClothingSlots
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.EARS;

    [DataField, AutoNetworkedField]
    public SquadLeaderTrackerMode? DefaultMode;

    [DataField, AutoNetworkedField]
    public HashSet<SquadLeaderTrackerMode> TrackerModes = new();
}
