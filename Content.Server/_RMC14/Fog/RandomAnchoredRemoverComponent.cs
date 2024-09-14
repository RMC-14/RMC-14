using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Fog;

/// <summary>
///     Marks any entities anchored on the same tile as eligible to be removed at round-start.
///     Only anchored entities that pass <see cref="Whitelist"/> will be deleted.
///     A group is randomly chosen from all the ones available, grouped by <see cref="Group"/>
/// </summary>
[RegisterComponent]
[Access(typeof(FogSystem))]
public sealed partial class RandomAnchoredRemoverComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Group = string.Empty;

    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist? Whitelist;
}
