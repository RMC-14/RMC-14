using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._RMC14.Marines.Roles.Ranks;

/// <summary>
///     Used for cosmetic ranks.
/// </summary>
[Prototype]
public sealed partial class RankPrototype : IPrototype, IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<RankPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    ///     The name of the rank.
    /// </summary>
    [AlwaysPushInheritance]
    [DataField(required: true)]
    public string Name { get; set; } = default!;

    /// <summary>
    ///     The shortened version of the rank.
    /// </summary>
    [AlwaysPushInheritance]
    [DataField(required: true)]
    public string ShortenedName { get; set; } = default!;

    [DataField]
    public HashSet<JobRequirement>? Requirements { get; set; }
}