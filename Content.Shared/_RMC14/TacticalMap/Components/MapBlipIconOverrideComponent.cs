using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent]
public sealed partial class MapBlipIconOverrideComponent : Component
{
    [DataField]
    public SpriteSpecifier.Rsi? Icon;
}
