using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

// ReSharper disable CheckNamespace

namespace Content.Shared.Alert;

public sealed partial class AlertPrototype : IInheritingPrototype
{
    [ParentDataField(typeof(PrototypeIdArraySerializer<AlertPrototype>))]
    public string[]? Parents { get; }

    [AbstractDataField]
    public bool Abstract { get; }
}
