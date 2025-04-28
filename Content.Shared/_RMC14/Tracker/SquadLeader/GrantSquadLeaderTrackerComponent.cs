using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadLeaderTrackerSystem))]
public sealed partial class GrantSquadLeaderTrackerComponent : Component, IClothingSlots
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.EARS;

    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype> DefaultMode;

    [DataField, AutoNetworkedField]
    public List<ProtoId<JobPrototype>> TrackableRoles= new();
}
