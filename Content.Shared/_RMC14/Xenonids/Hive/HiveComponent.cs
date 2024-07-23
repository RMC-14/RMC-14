using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<int, FixedPoint2> TierLimits = new()
    {
        [2] = 0.5,
        [3] = 0.2,
    };

    [DataField, AutoNetworkedField]
    public Dictionary<TimeSpan, List<EntProtoId>> Unlocks = new();

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> AnnouncedUnlocks = new();

    [DataField, AutoNetworkedField]
    public List<TimeSpan> AnnouncementsLeft = [];

    [DataField, AutoNetworkedField]
    public SoundSpecifier AnnounceSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_distantroar_3.ogg", AudioParams.Default.WithVolume(-6));

    [DataField, AutoNetworkedField]
    public bool SeeThroughContainers;

    [DataField, AutoNetworkedField]
    public TimeSpan? LastQueenDeath;

    [DataField]
    public TimeSpan NewQueenCooldown = TimeSpan.FromMinutes(5);

    [DataField, AutoNetworkedField]
    public TimeSpan? LastConstruct;

    [DataField]
    public TimeSpan NewConstructCooldown = TimeSpan.FromMinutes(5);

    /// <summary>
    /// How many of each hive construction can be built.
    /// If it reaches 0 it can no longer be built.
    /// If an entity being constructed is not present it will be logged as an error,
    /// use 0 to allow it to be constructed later.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public Dictionary<EntProtoId, int> ConstructionLimits = new();
}
