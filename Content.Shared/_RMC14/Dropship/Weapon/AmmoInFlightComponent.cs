using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Explosion.Implosion;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class AmmoInFlightComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates Target;

    [DataField, AutoNetworkedField]
    public bool SpawnedMarker;

    [DataField, AutoNetworkedField]
    public TimeSpan MarkerAt;

    [DataField, AutoNetworkedField]
    public EntityUid? Marker;

    // TODO RMC14 debris
    [DataField, AutoNetworkedField]
    public TimeSpan NextShot;

    [DataField, AutoNetworkedField]
    public TimeSpan ShotDelay = TimeSpan.FromSeconds(0.1);

    [DataField, AutoNetworkedField]
    public int ShotsLeft;

    [DataField, AutoNetworkedField]
    public int ShotsPerVolley;

    [DataField, AutoNetworkedField]
    public int SoundEveryShots = 3;

    [DataField, AutoNetworkedField]
    public int SoundShotsLeft;

    [DataField, AutoNetworkedField]
    public TimeSpan? PlayGroundSoundAt;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage;

    [DataField, AutoNetworkedField]
    public int ArmorPiercing = 10;

    [DataField, AutoNetworkedField]
    public int BulletSpread = 3;

    [DataField, AutoNetworkedField]
    public TimeSpan SoundTravelTime = TimeSpan.FromSeconds(1.1);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundMarker;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundGround;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundImpact;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundWarning;

    [DataField, AutoNetworkedField]
    public bool WarnedSound;

    [DataField, AutoNetworkedField]
    public bool MarkerWarning;

    [DataField, AutoNetworkedField]
    public EntityUid? WarningMarker;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan WarningMarkerAt;

    [DataField, AutoNetworkedField]
    public bool WarnedMarker;

    [DataField, AutoNetworkedField]
    public List<EntProtoId> ImpactEffects = new();

    [DataField, AutoNetworkedField]
    public RMCExplosion? Explosion;

    [DataField, AutoNetworkedField]
    public RMCImplosion? Implosion;

    [DataField, AutoNetworkedField]
    public RMCFire? Fire;
}
