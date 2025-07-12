using Content.Shared._RMC14.Prototypes;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

// ReSharper disable CheckNamespace
namespace Content.Shared.Roles;
// ReSharper restore CheckNamespace

public sealed partial class DepartmentPrototype : IInheritingPrototype, ICMSpecific
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<DepartmentPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [DataField]
    public bool IsCM { get; }

    [DataField]
    public string? CustomName;

    [DataField]
    public bool Hidden;

    /// <summary>
    /// RMC14 for logical communication of the department and its radio channel.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype>? DepartmentRadio { get; private set; }

    /// <summary>
    /// RMC14 to logical determine if a role is a department head.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? HeadOfDepartment { get; private set; }
}
