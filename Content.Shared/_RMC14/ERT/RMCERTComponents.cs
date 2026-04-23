using Robust.Shared.GameStates;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.ERT;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCERTDistressBeaconComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<RMCERTCallPrototype>> AllowedCalls = [];

    [DataField("requestTitle"), AutoNetworkedField]
    private LocId _requestTitle = "rmc-ert-beacon-request-title-handheld";

    public string RequestTitle => Loc.GetString(_requestTitle);

    [DataField("recipient")]
    private LocId _recipient = "rmc-ert-recipient-high-command";

    public string Recipient => Loc.GetString(_recipient);

    [DataField, AutoNetworkedField]
    public bool ReasonRequired = true;

    [DataField, AutoNetworkedField]
    public bool SingleUse = true;

    [DataField, AutoNetworkedField]
    public bool ResetOnDeny = true;

    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(10);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? LastUsed;

    [DataField, AutoNetworkedField]
    public bool Spent;

    [DataField, AutoNetworkedField]
    public int ReasonLimit = 200;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCERTShuttleComponent : Component
{
    [DataField, AutoNetworkedField]
    public string? Call;

    [DataField, AutoNetworkedField]
    public string? Organization;

    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>> NpcFactions = [];

    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent>? IffFaction;

    [DataField, AutoNetworkedField]
    public List<string> LandingTags = [];

    [DataField, AutoNetworkedField]
    public bool NoHijack = true;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCERTSeatComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> SeatTags = [];

    [DataField, AutoNetworkedField]
    public List<string> ReservedRoleTags = [];

    [DataField, AutoNetworkedField]
    public int Priority;

    [DataField, AutoNetworkedField]
    public NetEntity? OccupiedBy;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? ReservationExpires;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCERTMemberComponent : Component
{
    [DataField, AutoNetworkedField]
    public Guid RequestId;

    [DataField, AutoNetworkedField]
    public string Call = string.Empty;

    [DataField, AutoNetworkedField]
    public string Role = string.Empty;

    [DataField, AutoNetworkedField]
    public string Team = string.Empty;
}

[RegisterComponent]
public sealed partial class RMCERTSpawnPointComponent : Component
{
    [DataField]
    public List<string> RoleTags = [];

    [DataField]
    public List<string> SeatTags = [];

    [DataField]
    public int Priority;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCERTLandingZoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<string> Tags = [];

    [DataField, AutoNetworkedField]
    public List<string> DockClasses = [];

    [DataField, AutoNetworkedField]
    public bool ERTOnly = true;

    [DataField, AutoNetworkedField]
    public bool Enabled = true;
}
