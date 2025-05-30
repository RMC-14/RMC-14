using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Physics;
using Content.Shared.Tag;
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
    public bool IsDivider { get; set; } = false;

    [DataField]
    public ProtoId<RMCConstructionPrototype>[]? Listed { get; set; }

    [AlwaysPushInheritance]
    [DataField]
    public bool HasBuildRestriction { get; set; } = true;

    [AlwaysPushInheritance]
    [DataField]
    public CollisionGroup RestrictedCollisionGroup = CollisionGroup.Impassable;

    [AlwaysPushInheritance]
    [DataField]
    public ProtoId<TagPrototype>[]? RestrictedTags { get; set; }

    [DataField]
    public EntProtoId Prototype { get; set; } = default!;

    [AlwaysPushInheritance]
    [DataField]
    public int? MaterialCost { get; set; }

    [DataField]
    public HashSet<int>? StackAmounts { get; set; }

    [AlwaysPushInheritance]
    [DataField]
    public EntProtoId<SkillDefinitionComponent>? Skill { get; set; }

    [AlwaysPushInheritance]
    [DataField]
    public int SkillLevel { get; set; } = 1;

    [AlwaysPushInheritance]
    [DataField]
    public TimeSpan DoAfterTime { get; set; } = TimeSpan.Zero;

    [AlwaysPushInheritance]
    [DataField]
    public TimeSpan DoAfterTimeMin { get; set; } = TimeSpan.Zero;
}
