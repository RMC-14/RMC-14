using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

// ReSharper disable CheckNamespace
namespace Content.Shared.Decals;
// ReSharper restore CheckNamespace

public sealed partial class DecalPrototype : IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<DepartmentPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [DataField]
    public bool IsCM;
}
