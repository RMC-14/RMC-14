using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunUnskilledPenaltyComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Firearms = 1;

    [DataField, AutoNetworkedField]
    public Angle AngleIncrease = Angle.FromDegrees(10);
}
