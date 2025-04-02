using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Explosion;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class DropshipAmmoComponent : Component
{
    [DataField, AutoNetworkedField]
    public int TargetSpread = 3;

    [DataField, AutoNetworkedField]
    public int BulletSpread = 3;

    [DataField(required: true), AutoNetworkedField]
    public int Rounds = 400;

    [DataField(required: true), AutoNetworkedField]
    public int MaxRounds = 400;

    [DataField(required: true), AutoNetworkedField]
    public int RoundsPerShot = 40;

    [DataField, AutoNetworkedField]
    public int ShotsPerVolley = 1;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage;

    [DataField, AutoNetworkedField]
    public int ArmorPiercing = 10;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<DropshipWeaponComponent> Weapon = new("");

    [DataField, AutoNetworkedField]
    public TimeSpan TravelTime = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan SoundTravelTime = TimeSpan.FromSeconds(1.1);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundCockpit;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundMarker;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundGround;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundImpact;

    [DataField, AutoNetworkedField]
    public EntProtoId? ImpactEffect;

    [DataField, AutoNetworkedField]
    public string? AmmoType;

    [DataField, AutoNetworkedField]
    public RMCExplosion? Explosion;

    [DataField, AutoNetworkedField]
    public RMCFire? Fire;

    [DataField, AutoNetworkedField]
    public int SoundEveryShots = 3;

    [DataField, AutoNetworkedField]
    public bool DeleteOnEmpty;
}

[Serializable, NetSerializable]
public enum DropshipAmmoVisuals
{
    Fill,
}
