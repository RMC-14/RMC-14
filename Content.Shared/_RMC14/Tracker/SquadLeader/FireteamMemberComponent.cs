using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SquadLeaderTrackerSystem))]
public sealed partial class FireteamMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Fireteam;

    [DataField, AutoNetworkedField]
    public bool Leader;
}
