using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCCanUseBroilerComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId ExamineText = "rmc-broiler-action-examine";
}
