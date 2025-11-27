using Content.Shared.Item;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Emplacements;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeaponMountComponent : Component
{
    /// <summary>
    ///     The whitelist of what is allowed to be mounted. //TODO implement this
    /// </summary>
    [DataField]
    public List<EntProtoId> AttachablePrototypes = new();

    /// <summary>
    ///     The prototype that spawns permanently attached to the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? FixedWeaponPrototype;

    /// <summary>
    ///     The entity currently mounted on top of the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? MountedEntity;

    /// <summary>
    ///     The entity currently using the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    ///     Whether the mounted entity is secured to the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsWeaponSecured;

    /// <summary>
    ///     Whether the weapon can be removed from the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsWeaponLocked;

    /// <summary>
    ///     The DoAfter duration for any assembling related actions.
    /// </summary>
    [DataField]
    public TimeSpan AssembleDelay = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    ///     The DOAfter duration for any disassembling related actions.
    /// </summary>
    [DataField]
    public TimeSpan DisassembleDelay = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    ///     The tool quality required to rotate the mount.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> RotationTool = "Anchoring";

    /// <summary>
    ///     The tool quality required to remove a mounted entity from the mount.
    /// </summary>
    [DataField]
    public ProtoId<ToolQualityPrototype> DismantlingTool = "Prying";

    /// <summary>
    ///     The id for the container the weapon will be stored in.
    /// </summary>
    [DataField]
    public string WeaponSlotId = "weapon";

    /// <summary>
    ///     The item size while nothing is attached to the mount.
    /// </summary>
    [DataField]
    public ProtoId<ItemSizePrototype> MountSize = "Normal";

    /// <summary>
    ///     The item size while an entity is attached to the mount.
    /// </summary>
    [DataField]
    public ProtoId<ItemSizePrototype> MountedWeaponSize = "Huge";

    /// <summary>
    ///     The distance in which no other weapon mounts can be placed.
    /// </summary>
    [DataField]
    public int ExclusionAreaSize = 5;

    /// <summary>
    ///     Whether the mount can ignore the check for nearby other mounts.
    /// </summary>
    [DataField]
    public bool CanPlaceNearOtherMounts;

    [DataField]
    public SoundSpecifier ScrewSound = new SoundPathSpecifier("/Audio/Items/screwdriver.ogg");

    [DataField]
    public SoundSpecifier WrenchSound = new SoundPathSpecifier("/Audio/Items/ratchet.ogg");

    [DataField]
    public SoundSpecifier PrySound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");

    [DataField]
    public SoundSpecifier SecureSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");
}

[Serializable, NetSerializable]
public enum WeaponMountComponentVisualLayers : byte
{
    Mounted,
    MountedAmmo,
    Folded,
    FoldedAmmo,
}
