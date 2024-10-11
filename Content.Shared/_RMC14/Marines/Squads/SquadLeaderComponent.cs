using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Squads;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SquadSystem))]
public sealed partial class SquadLeaderComponent : Component;
