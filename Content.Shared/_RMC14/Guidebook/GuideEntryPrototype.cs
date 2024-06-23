using Content.Shared._RMC14.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

// ReSharper disable once CheckNamespace
namespace Content.Shared.Guidebook;

public sealed partial class GuideEntryPrototype : IInheritingPrototype, ICMSpecific
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<GuideEntryPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [DataField]
    public bool IsCM { get; }
}
