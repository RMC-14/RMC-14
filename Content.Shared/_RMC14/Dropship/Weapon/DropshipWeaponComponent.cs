using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Dropship.Weapon;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedDropshipWeaponSystem))]
public sealed partial class DropshipWeaponComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Abbreviation = string.Empty;

    [DataField, AutoNetworkedField]
    public TimeSpan FireDelay = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextFireAt;

    [DataField, AutoNetworkedField]
    public bool FireInTransport;

    [DataField, AutoNetworkedField]
    public SkillWhitelist? Skills;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? WeaponAttachedSprite;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? AmmoEmptyAttachedSprite;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi? AmmoAttachedSprite;

    /// <summary>
    ///     Specifies the ammo threshold at which the loaded ammo sprite changes.
    ///     Each threshold in this list requires a sprite with the base name of the fully loaded sprite, followed by "_ammo_count",
    ///     where "ammo_count" should be replaced with the actual number of rounds.
    ///     Thresholds are only used when the weapon is neither fully loaded nor empty, so the fully loaded and empty states do not need to be included in this list.
    ///     If the ammo amount does not exactly match any threshold, the sprite for the next lower threshold will be used.
    ///     For example, if the thresholds are 100 and 200 rounds, and 180 rounds are loaded, the sprite for 100 rounds will be used.
    /// </summary>
    /// <example>
    ///     If the fully loaded sprite is named "minirocket_pod_loaded",
    ///     the sprite for 5 rockets loaded should be named "minirocket_pod_loaded_5".
    /// </example>
    [DataField, AutoNetworkedField]
    public List<int> AmmoSpriteThresholds = new ();
}

[Serializable, NetSerializable]
public enum DropshipWeaponVisuals
{
    Sprite,
    State,
}
