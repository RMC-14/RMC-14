// ReSharper disable CheckNamespace

using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared.Access;
public sealed partial class AccessLevelPrototype : IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<AccessLevelPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    ///     Denotes what faction this access belongs to.
    /// </summary>
    [DataField]
    public EntProtoId<IFFFactionComponent>? Faction;

    /// <summary>
    ///     Denotes what access group this access belongs to.
    /// </summary>
    [DataField]
    public string AccessGroup = "";

    /// <summary>
    ///     Denotes if this Access is Hidden from the Modification Console.
    /// </summary>
    [DataField]
    public bool Hidden = false;
}
