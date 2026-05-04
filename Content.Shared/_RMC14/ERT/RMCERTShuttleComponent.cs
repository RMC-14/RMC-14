using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Marks a shuttle grid as belonging to an active ERT request and carries routing metadata onto the shuttle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCERTShuttleComponent : Component
{
    /// <summary>
    /// Request that owns this shuttle until arrival, return or cleanup.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Guid RequestId;

    /// <summary>
    /// Prototype id of the selected ERT call that spawned or configured this shuttle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Call;

    /// <summary>
    /// Localized organization label copied from the selected call.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Organization;

    /// <summary>
    /// NPC factions assigned to responders and propagated to the shuttle for routing context.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>> NpcFactions = [];

    /// <summary>
    /// Optional IFF faction associated with this shuttle's responders.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent>? IffFaction;

    /// <summary>
    /// Landing destination tags this shuttle may use while under ERT control.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> LandingTags = [];

    /// <summary>
    /// Whether normal hijack behavior is blocked for this ERT shuttle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NoHijack = true;
}
