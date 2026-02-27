using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Telephone;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCTelephoneSystem))]
public sealed partial class RotaryPhoneDndComponent : Component;
