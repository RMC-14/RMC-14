using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCCanUseBroilerComponent : Component;
