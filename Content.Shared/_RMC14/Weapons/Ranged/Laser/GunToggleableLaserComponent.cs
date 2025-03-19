using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Weapons.Ranged.Laser;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GunToggleableLaserComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public List<GunToggleableLaserSetting> Settings = new();

    [DataField, AutoNetworkedField]
    public int Setting;

    /// <summary>
    ///     If the laser is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Active = true;

    /// <summary>
    ///     The action prototype belonging to this action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionToggleLaser";

    /// <summary>
    ///     The action id.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    /// <summary>
    ///     The sound to play when this action is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");

    /// <summary>
    ///     The duration multiplier to apply during aimed shot, while the laser is active.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AimDurationMultiplier = 0.6f;

    /// <summary>
    ///     The value to subtract from the duration multiplier if the laser is active and the target is spotted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpottedAimDurationMultiplierSubtraction = 0.15f;
}

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct GunToggleableLaserSetting(SpriteSpecifier.Rsi Icon);
