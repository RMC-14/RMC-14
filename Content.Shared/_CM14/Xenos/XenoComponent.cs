using Content.Shared.Access;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared._CM14.Xenos;

// TODO CM14 split up this component
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoComponent : Component
{
    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> EvolvesTo = new();

    [DataField, AutoNetworkedField]
    public List<EntProtoId> ActionIds = new();

    [DataField]
    public Dictionary<EntProtoId, EntityUid> Actions = new();

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int Plasma;

    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxPlasma = 300;

    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int PlasmaRegen;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PlasmaRegenCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPlasmaRegenTime;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int? OriginalDrawDepth;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AcidDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string DevourContainerId = "cm_xeno_devour";

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DevourDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier RegurgitateSound = new SoundPathSpecifier("/Audio/_CM14/Xeno/alien_drool2.ogg");

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Hive;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId TailAnimationId = "WeaponArcThrust";

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float TailRange = 3;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier TailDamage = new();

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TailHitSound = new SoundCollectionSpecifier("XenoTailSwipe");

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public float BuildRange = 1;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> CanBuild = new();

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? BuildChoice;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BuildDelay = TimeSpan.FromSeconds(4);

    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string> AccessLevels = new() { "Xeno" };
}
