// ReSharper disable CheckNamespace

using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Access;
public sealed partial class AccessGroupPrototype : IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<AccessGroupPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    ///     Denotes what faction this group belongs to.
    /// </summary>
    [DataField]
    public EntProtoId<IFFFactionComponent>? Faction;

    /// <summary>
    ///     Denotes what group this group is listed under.
    /// </summary>
    [DataField]
    public string AccessGroup = "";

    /// <summary>
    ///     Denotes if this group is Hidden from the Modification Console.
    /// </summary>
    [DataField]
    public bool Hidden = false;
}
