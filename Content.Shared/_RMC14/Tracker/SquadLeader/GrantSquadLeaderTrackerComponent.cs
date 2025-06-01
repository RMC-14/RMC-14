using Content.Shared.Inventory;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadLeaderTrackerSystem))]
public sealed partial class GrantSquadLeaderTrackerComponent : Component, IClothingSlots
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.EARS;

    [DataField(required: true), AutoNetworkedField]
    public ProtoId<TrackerModePrototype> DefaultMode;

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<TrackerModePrototype>> TrackerModes = new();
}
