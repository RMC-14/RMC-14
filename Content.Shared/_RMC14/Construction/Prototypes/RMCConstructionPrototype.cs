using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._RMC14.Construction.Prototypes;

[Prototype("rmcConstruction")]
public sealed partial class RMCConstructionPrototype : IPrototype, IInheritingPrototype
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<RMCConstructionPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [AlwaysPushInheritance]
    [DataField(required: true)]
    public string Name { get; set; } = default!;

    [DataField]
    public bool IsDivider = false;

    [DataField]
    public ProtoId<RMCConstructionPrototype>[]? Listed { get; set; }

    [DataField]
    public bool HasBuildRestriction = true;

    [DataField]
    public EntProtoId Prototype { get; set; } = default!;

    [DataField]
    public int? MaterialCost { get; set; }

    [DataField]
    public HashSet<int>? StackAmounts { get; set; }

    [DataField]
    public EntProtoId<SkillDefinitionComponent>? Skill { get; set; }

    [DataField]
    public int SkillLevel { get; set; } = 1;

    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan DoAfterTimeMin = TimeSpan.Zero;
}
