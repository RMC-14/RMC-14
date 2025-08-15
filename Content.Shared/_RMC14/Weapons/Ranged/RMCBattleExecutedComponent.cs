using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCBattleExecutedComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId ExecutedText = "rmc-executed";
}
