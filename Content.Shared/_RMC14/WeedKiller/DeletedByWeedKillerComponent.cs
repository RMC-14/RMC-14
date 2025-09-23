using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.WeedKiller;

[RegisterComponent, NetworkedComponent]
[Access(typeof(WeedKillerSystem))]
public sealed partial class DeletedByWeedKillerComponent : Component;
