using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Roles.Ranks;

/// <summary>
///     Used for cosmetic ranks.
/// </summary>
[Prototype]
public sealed partial class RankPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of the rank.
    /// </summary>
    [DataField(required: true)]
    public string Name { get; set; } = default!;

    /// <summary>
    ///     The shortened version of the rank.
    /// </summary>
    [DataField(required: true)]
    public string ShortenedName { get; set; } = default!;

    /// <summary>
    ///     The jobs that this rank applies to.
    /// </summary>
    [DataField(required: true)]
    public List<ProtoId<JobPrototype>> Jobs { get; private set; } = [];
}