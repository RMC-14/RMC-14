using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class AmmoInFlightComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Ammo;

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
    public int SoundEveryShots = 3;

    [DataField, AutoNetworkedField]
    public int SoundShotsLeft;

    [DataField, AutoNetworkedField]
    public TimeSpan? PlayGroundSoundAt;
}
