using Content.Shared.Access;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
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
    public FixedPoint2 Plasma;

    [DataField(required: true), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int MaxPlasma = 300;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PlasmaTransferDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier PlasmaTransferSound = new SoundCollectionSpecifier("XenoDrool");

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PlasmaRegenOnWeeds;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 HealthRegenOnWeeds = 1.25;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan PlasmaRegenCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextRegenTime;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AcidDelay = TimeSpan.FromSeconds(5);

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public string DevourContainerId = "cm_xeno_devour";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan DevourDelay = TimeSpan.FromSeconds(5);

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier RegurgitateSound = new SoundCollectionSpecifier("XenoDrool");

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid Hive;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId TailAnimationId = "WeaponArcThrust";

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TailRange = 3;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier TailDamage = new();

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier TailHitSound = new SoundCollectionSpecifier("XenoTailSwipe");

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 BuildRange = 1;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> CanBuild = new();

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? BuildChoice;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan BuildDelay = TimeSpan.FromSeconds(4);

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 OrderConstructionRange = 1.5;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public List<EntProtoId> CanOrderConstruction = new();

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityCoordinates? OrderingConstructionAt;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OrderConstructionDelay = TimeSpan.FromSeconds(3);

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan OrderConstructionAddPlasmaDelay = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(PrototypeIdHashSetSerializer<AccessLevelPrototype>))]
    [ViewVariables(VVAccess.ReadWrite)]
    public HashSet<string> AccessLevels = new() { "Xeno" };

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int PheromonesPlasmaCost = 35;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PheromonesPlasmaUpkeep = 2.5;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextPheromonesPlasmaUse;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int PheromonesRange = 8;

    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 PheromonesMultiplier = 1;

    [DataField, AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool OnWeeds;
}
