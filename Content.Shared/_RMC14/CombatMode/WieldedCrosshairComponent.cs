using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.CombatMode;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCCombatModeSystem))]
public sealed partial class WieldedCrosshairComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? Rsi;
}
