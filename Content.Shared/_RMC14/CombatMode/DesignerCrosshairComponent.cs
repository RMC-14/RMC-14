using Content.Shared._RMC14.Xenonids.Designer;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.CombatMode;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DesignerCrosshairComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? OptimizedWallRsi;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? OptimizedDoorRsi;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? FlexibleWallRsi;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? FlexibleDoorRsi;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? ConstructWallRsi;

    [DataField(required: true), AutoNetworkedField]
    public SpriteSpecifier.Rsi? ConstructDoorRsi;
}
