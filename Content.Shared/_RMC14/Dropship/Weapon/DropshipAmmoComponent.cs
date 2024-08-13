using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class DropshipAmmoComponent : Component
{
    [DataField, AutoNetworkedField]
    public int MaxSpread = 3;

    [DataField(required: true), AutoNetworkedField]
    public int Rounds = 400;

    [DataField(required: true), AutoNetworkedField]
    public int MaxRounds = 400;

    [DataField(required: true), AutoNetworkedField]
    public int RoundsPerShot = 40;

    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField(required: true), AutoNetworkedField]
    public int ArmorPiercing = 10;

    [DataField(required: true), AutoNetworkedField]
    public EntProtoId<DropshipWeaponComponent> Weapon = new("");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundCockpit;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundGround;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SoundImpact;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastImpactSoundAt;
}
