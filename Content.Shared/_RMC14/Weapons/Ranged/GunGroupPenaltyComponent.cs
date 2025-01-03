using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunGroupPenaltyComponent : Component
{
    [DataField, AutoNetworkedField]
    public Angle AngleIncrease = Angle.FromDegrees(75);

    [DataField, AutoNetworkedField]
    public float Recoil = 10;

    [DataField, AutoNetworkedField]
    public float DamageMultiplier = 0.5f;
}
