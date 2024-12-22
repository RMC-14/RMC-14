using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMGunSystem))]
public sealed partial class GunDualWieldingComponent : Component
{
    [DataField, AutoNetworkedField]
    public GunDualWieldingGroup WeaponGroup = GunDualWieldingGroup.None;

    [DataField, AutoNetworkedField]
    public Angle ScatterModifier = Angle.FromDegrees(16);

    [DataField, AutoNetworkedField]
    public FixedPoint2 AccuracyAddMult = -0.6;

    [DataField, AutoNetworkedField]
    public float RecoilModifier = 1;

    // TODO: RMC 14, figure out how to make prediction not fuck up CM-style dual-wielding.
    //[DataField, AutoNetworkedField]
    //public string AmmoContainerID = "gun_magazine";
}

public enum GunDualWieldingGroup : byte
{
    None = 0,
    Handgun = 1 << 0,
    Submachinegun = 1 << 1,
    Rifle = 1 << 2,
    Shotgun = 1 << 3,
    Heavy = 1 << 4
}
