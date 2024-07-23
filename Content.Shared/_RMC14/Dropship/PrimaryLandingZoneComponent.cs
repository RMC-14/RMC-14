using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDropshipSystem))]
public sealed partial class PrimaryLandingZoneComponent : Component;
