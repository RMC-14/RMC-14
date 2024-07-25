using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged.Ammo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(GunToggleableAmmoSystem))]
public sealed partial class GunToggleableAmmoComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<GunToggleableAmmoSetting> Settings = new();

    [DataField, AutoNetworkedField]
    public int Setting;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionToggleAmmo";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct GunToggleableAmmoSetting(DamageSpecifier Damage, int ArmorPiercing, LocId Name, SpriteSpecifier.Rsi Icon);
