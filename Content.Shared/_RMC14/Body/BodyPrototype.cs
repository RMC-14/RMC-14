using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

// ReSharper disable once CheckNamespace
namespace Content.Shared.Body.Prototypes;

public sealed partial class BodyPrototype : IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<BodyPrototype>))]
    public string[]? Parents { get; }

    [AbstractDataField]
    public bool Abstract { get; }
}
