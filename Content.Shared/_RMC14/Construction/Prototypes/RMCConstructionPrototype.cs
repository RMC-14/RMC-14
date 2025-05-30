using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
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

    [NeverPushInheritance]
    [DataField]
    public bool IsDivider { get; set; } = false;

    [DataField]
    public bool NoRotate { get; set; } = false;

    /// <summary>
    /// Which other construction prototypes are listed when this button is pressed.
    /// Useful for things like comfy chairs which have multiple variants.
    /// </summary>
    [DataField]
    public ProtoId<RMCConstructionPrototype>[]? Listed { get; set; }

    [AlwaysPushInheritance]
    [DataField]
    public EntityWhitelist? Whitelist { get; set; }

    [AlwaysPushInheritance]
    [DataField]
    public EntityWhitelist? Blacklist { get; set; }

    [AlwaysPushInheritance]
    [DataField]
    public EntProtoId<SkillDefinitionComponent>? Skill { get; set; }

    [AlwaysPushInheritance]
    [DataField]
    public int RequiredSkillLevel { get; set; } = 1;

    [AlwaysPushInheritance]
    [DataField]
    public TimeSpan DoAfterTime { get; set; } = TimeSpan.Zero;

    [AlwaysPushInheritance]
    [DataField]
    public TimeSpan DoAfterTimeMin { get; set; } = TimeSpan.Zero;

    [AlwaysPushInheritance]
    [DataField]
    public CollisionGroup? RestrictedCollisionGroup = CollisionGroup.Impassable;

    [AlwaysPushInheritance]
    [DataField]
    public ProtoId<TagPrototype>[]? RestrictedTags { get; set; }

    [AlwaysPushInheritance]
    [DataField]
    public bool IgnoreBuildRestrictions = false;

    [DataField]
    public EntProtoId Prototype { get; set; } = default!;

    [AlwaysPushInheritance]
    [DataField]
    public int? MaterialCost { get; set; }

    /// <summary>
    /// How many objects spawn when this prototype is crafted.
    /// </summary>
    [DataField]
    public int Amount { get; set; } = 1;

    /// <summary>
    /// List of all the possible stack amounts that appear in the construction menu.
    /// </summary>
    [DataField]
    public HashSet<int>? StackAmounts { get; set; }
}
