using System.Numerics;
using Content.Shared.Access;
using Content.Shared.Alert;
using Content.Shared.Chat.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoSystem))]
public sealed partial class XenoComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<JobPrototype> Role;

    [DataField, AutoNetworkedField]
    public List<EntProtoId> ActionIds = new();

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId, EntityUid> Actions = new();

    [DataField, AutoNetworkedField]
    public FixedPoint2 FlatHealing = 0.5;

    [DataField, AutoNetworkedField]
    public FixedPoint2 CritHealMultiplier = 0.33;

    [DataField, AutoNetworkedField]
    public FixedPoint2 RestHealMultiplier = 1;

    [DataField, AutoNetworkedField]
    public FixedPoint2 StandHealingMultiplier = 0.4;

    [DataField, AutoNetworkedField]
    public float MaxHealthDivisorHeal = 65;

    [DataField, AutoNetworkedField]
    public bool HealOffWeeds;

    [DataField, AutoNetworkedField]
    public TimeSpan RegenCooldown = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextRegenTime;

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> AccessLevels = new() { "CMAccessXeno" };

    [DataField, AutoNetworkedField]
    public int Tier;

    [DataField, AutoNetworkedField]
    public Vector2 HudOffset;

    [DataField, AutoNetworkedField]
    public bool ContributesToVictory = true;

    [DataField, AutoNetworkedField]
    public bool CountedInSlots = true;

    [DataField, AutoNetworkedField]
    public bool BypassTierCount;

    [DataField, AutoNetworkedField]
    public TimeSpan UnlockAt = TimeSpan.FromSeconds(60);

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> ArmorAlert = "XenoArmor";

    [DataField, AutoNetworkedField]
    public bool SpawnAtLeaderPoint;

    [DataField, AutoNetworkedField]
    public ProtoId<EmoteSoundsPrototype>? EmoteSounds = "Xeno";

    [DataField, AutoNetworkedField]
    public bool MuteOnSpawn;

    /// <summary>
    /// Hides this xeno from the caste unlock announcements.
    /// Use for admeme or unimplemented castes that can't be evolved to.
    /// </summary>
    [DataField]
    public bool Hidden;

    public EmoteSoundsPrototype? Sounds;
}
