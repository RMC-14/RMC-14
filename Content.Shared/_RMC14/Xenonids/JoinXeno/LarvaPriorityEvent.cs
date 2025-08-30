using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[ByRefEvent]
public record struct AssignLarvaPriorityEvent(EntityUid Larva, NetUserId? OriginalParasiteUserId, NetUserId? BurstVictimUserId);

[ByRefEvent]
public record struct LarvaPriorityCompletedEvent(EntityUid Larva, bool WasAccepted, NetUserId? UserId);

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParasiteInfectionTrackingComponent : Component
{
    [DataField, AutoNetworkedField]
    public NetUserId? OriginalParasiteUserId;
}
