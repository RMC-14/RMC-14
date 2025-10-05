using Content.Shared._RMC14.Construction.Prototypes;
using Content.Shared._RMC14.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

// ReSharper disable once CheckNamespace
namespace Content.Shared.Construction.Prototypes;

public sealed partial class ConstructionPrototype : IInheritingPrototype, ICMSpecific
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ConstructionPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [DataField]
    public bool IsCM { get; }

    [DataField("rmcPrototype")]
    public ProtoId<RMCConstructionPrototype>? RMCPrototype { get; }

    [DataField]
    public Color IconColor = Color.FromHex("#ffffff");
}
