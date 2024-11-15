using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Construction.Upgrades;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCUpgradeSystem))]
public sealed partial class RMCConstructionUpgradeItemComponent : Component;
