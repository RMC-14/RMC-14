using Content.Shared._RMC14.Marines.Roles.Ranks;
using Content.Shared._RMC14.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

// ReSharper disable CheckNamespace
namespace Content.Shared.Roles;
// ReSharper restore CheckNamespace

public sealed partial class JobPrototype : IInheritingPrototype, ICMSpecific
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<JobPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [DataField]
    public bool IsCM { get; }

    [DataField]
    public readonly bool HasSquad;

    [DataField]
    public readonly bool HasIcon = true;

    [DataField]
    public readonly bool Hidden;

    [DataField]
    public readonly int? OverwatchSortPriority;

    [DataField]
    public readonly bool OverwatchShowName;

    [DataField]
    public readonly string? OverwatchRoleName;

    [DataField]
    public readonly string? SpawnMenuRoleName;

    [DataField]
    public readonly Dictionary<ProtoId<RankPrototype>, HashSet<JobRequirement>?>? Ranks;

    [DataField]
    public float RoleWeight;

    [DataField]
    public ProtoId<StartingGearPrototype>? DummyStartingGear { get; private set; }

    [DataField]
    public LocId? Greeting;

    /// <summary>
    /// RMC14 for arrival notification sound if <see cref="JoinNotifyCrew"/> true.
    /// </summary>
    [DataField]
    public SoundSpecifier LatejoinArrivalSound { get; private set; } = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/sound_misc_boatswain.ogg");
}
