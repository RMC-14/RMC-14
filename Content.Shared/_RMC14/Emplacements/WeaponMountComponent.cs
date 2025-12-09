using Content.Shared.Item;
using Content.Shared.Tools;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._RMC14.Emplacements;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class WeaponMountComponent : Component
{
    /// <summary>
    ///     The whitelist that determines what entities are allowed to be mounted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? MountableWhitelist;

    /// <summary>
    ///     The prototype that spawns attached to the mount.
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
    [DataField, AutoNetworkedField]
    public TimeSpan AssembleDelay = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    ///     The DOAfter duration for any disassembling related actions.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan DisassembleDelay = TimeSpan.FromSeconds(1.5f);

    /// <summary>
    ///     The tool quality required to rotate the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> RotationTool = "Anchoring";

    /// <summary>
    ///     The tool quality required to remove a mounted entity from the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> DismantlingTool = "Prying";

    /// <summary>
    ///     The id for the container the weapon will be stored in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string WeaponSlotId = "weapon";

    /// <summary>
    ///     The item size while nothing is attached to the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype> MountSize = "Normal";

    /// <summary>
    ///     The item size while an entity is attached to the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ItemSizePrototype> MountedWeaponSize = "Huge";

    /// <summary>
    ///     The distance required from the closest mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MountExclusionAreaSize = 5;

    /// <summary>
    ///     The distance required from the closest barricade.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int BarricadeExclusionAreaSize;

    /// <summary>
    ///     The action prototype to stop using the mount.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string? DismountAction = "RMCActionDismount";

    /// <summary>
    ///     The uid of the action that makes you stop using the mount.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? DismountActionEntity;

    /// <summary>
    ///     Whether the mount can be rotated without the use of any tools.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanRotateWithoutTool;

    /// <summary>
    ///     Whether the user will mount the emplacement automatically after deploying it from their hand.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool MountOnDeploy;

    /// <summary>
    ///     Whether the mount is currently in a broken state.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Broken;

    [DataField]
    public SoundSpecifier UndeploySound = new SoundPathSpecifier("/Audio/Items/screwdriver.ogg");

    [DataField]
    public SoundSpecifier RotateSound = new SoundPathSpecifier("/Audio/Items/ratchet.ogg");

    [DataField]
    public SoundSpecifier DetachSound = new SoundPathSpecifier("/Audio/Items/crowbar.ogg");

    [DataField]
    public SoundSpecifier SecureSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    [DataField]
    public SoundSpecifier? DeploySound;
}

[Serializable, NetSerializable]
public enum WeaponMountComponentVisualLayers : byte
{
    Mounted,
    MountedAmmo,
    Folded,
    FoldedAmmo,
    Broken,
    Overheated,
}
