using Content.Shared._RMC14.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

// ReSharper disable CheckNamespace
namespace Content.Shared.Roles;
// ReSharper restore CheckNamespace

public sealed partial class JobPrototype : IInheritingPrototype, ICMSpecific
{
    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<JobPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    [DataField]
    public bool IsCM { get; }

    [DataField]
    public readonly bool HasSquad;

    [DataField]
    public readonly bool HasIcon = true;

    [DataField]
    public readonly bool Hidden;

    [DataField]
    public readonly int? OverwatchSortPriority;

    [DataField]
    public readonly bool OverwatchShowName;

    [DataField]
    public readonly string? OverwatchRoleName;

    [DataField]
    public SpriteSpecifier.Rsi? MinimapIcon;

    [DataField]
    public SpriteSpecifier.Rsi? MinimapBackground;

    [DataField]
    public float RoleWeight;
}
