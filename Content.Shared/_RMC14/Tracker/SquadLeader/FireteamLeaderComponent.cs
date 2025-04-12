using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tracker.SquadLeader;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SquadLeaderTrackerSystem))]
public sealed partial class FireteamLeaderComponent : Component;
