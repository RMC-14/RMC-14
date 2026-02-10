using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.CombatMode;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCCombatModeSystem))]
public sealed partial class DesignerCrosshairComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? WallRsi;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? DoorRsi;
}
