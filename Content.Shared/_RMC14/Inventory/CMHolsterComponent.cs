using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Inventory;

// TODO RMC14 add to rifle holster
// TODO RMC14 add to machete scabbard pouch
// TODO RMC14 add to all large scabbards (machete scabbard, katana scabbard, m63 holster rig)
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class CMHolsterComponent : Component
{
    // List of entities "inside" the holster
    [DataField]
    public List<EntityUid> Contents = new();

    // How many entities fit "inside" the holster
    [DataField]
    public int HolsterSize = 1;

    /// <summary>
    /// Sound played whenever an entity is inserted into holster.
    /// </summary>
    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagIn/revolver_magin.ogg");

    /// <summary>
    /// Sound played whenever an entity is removed from holster.
    /// </summary>
    [DataField]
    public SoundSpecifier? EjectSound = new SoundPathSpecifier("/Audio/Weapons/Guns/MagOut/revolver_magout.ogg");

    // TODO: add whitelist to account for e.g. crowbars being "holstered" into combat toolbelts
}
