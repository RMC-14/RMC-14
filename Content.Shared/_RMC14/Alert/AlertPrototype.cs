using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

// ReSharper disable CheckNamespace

namespace Content.Shared.Alert;

public sealed partial class AlertPrototype : IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<AlertPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    public bool Abstract { get; private set; }
}
