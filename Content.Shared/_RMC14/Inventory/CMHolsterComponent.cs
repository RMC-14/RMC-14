using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Inventory;

// TODO RMC14 add to rifle holster
// TODO RMC14 add to machete scabbard pouch
// TODO RMC14 add to all large scabbards (machete scabbard, katana scabbard, m63 holster rig)
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class CMHolsterComponent : Component
{
    // List of entities "inside" the holster
    [DataField]
    public List<EntityUid> Contents = new();

    // Whitelist for entities that can be (un)holstered
    // Note: this does not block them from being inserted (use other whitelists for that),
    //  this is here to prevent "unholstering" non-weapons (e.g. tools from the combat toolbelt)
    [DataField]
    public EntityWhitelist? Whitelist;

    // Variables to mitigate holster spamming
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastEjectAt;

    [DataField, AutoNetworkedField]
    public TimeSpan? Cooldown;

    [DataField, AutoNetworkedField]
    public string? CooldownPopup;

    /// <summary>
    /// Sound played whenever an entity is inserted into holster.
    /// </summary>
    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/gun_pistol_sheathe.ogg");

    /// <summary>
    /// Sound played whenever an entity is removed from holster.
    /// </summary>
    [DataField]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/gun_pistol_draw.ogg");
}
