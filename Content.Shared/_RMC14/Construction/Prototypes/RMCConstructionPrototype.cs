using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Physics;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._RMC14.Construction.Prototypes;

[Prototype("rmcConstruction"), Serializable, NetSerializable]
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

    [AlwaysPushInheritance]
    [DataField]
    public bool NoRotate = false;

    /// <summary>
    /// Which other construction prototypes are listed when this button is pressed.
    /// Useful for things like comfy chairs which have multiple variants.
    /// </summary>
    [DataField]
    public ProtoId<RMCConstructionPrototype>[]? Listed;

    [AlwaysPushInheritance]
    [DataField]
    public EntProtoId<SkillDefinitionComponent>? Skill;

    [DataField]
    public EntProtoId<SkillDefinitionComponent> DelaySkill = "RMCSkillConstruction";

    [AlwaysPushInheritance]
    [DataField]
    public int RequiredSkillLevel = 1;

    [AlwaysPushInheritance]
    [DataField]
    public TimeSpan DoAfterTime = TimeSpan.Zero;

    [AlwaysPushInheritance]
    [DataField]
    public TimeSpan DoAfterTimeMin = TimeSpan.Zero;

    [AlwaysPushInheritance]
    [DataField]
    public CollisionGroup? RestrictedCollisionGroup = CollisionGroup.Impassable;

    [AlwaysPushInheritance]
    [DataField]
    public ProtoId<TagPrototype>[]? RestrictedTags;

    [AlwaysPushInheritance]
    [DataField]
    public bool IgnoreBuildRestrictions = false;

    [DataField]
    public EntProtoId Prototype = default!;

    [AlwaysPushInheritance]
    [DataField]
    public int? MaterialCost;

    /// <summary>
    /// How many objects spawn when this prototype is crafted.
    /// </summary>
    [DataField]
    public int Amount = 1;

    /// <summary>
    /// List of all the possible stack amounts that appear in the construction menu.
    /// </summary>
    [DataField]
    public HashSet<int>? StackAmounts;
}
