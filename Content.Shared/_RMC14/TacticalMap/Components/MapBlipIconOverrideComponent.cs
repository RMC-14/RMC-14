using Content.Shared._RMC14.Marines.Skills.Pamphlets;
using Content.Shared._RMC14.Vendors;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTacticalMapSystem), typeof(SharedCMAutomatedVendorSystem), typeof(SkillPamphletSystem))]
public sealed partial class MapBlipIconOverrideComponent : Component
{
    [DataField]
    public SpriteSpecifier.Rsi? Icon;
}
