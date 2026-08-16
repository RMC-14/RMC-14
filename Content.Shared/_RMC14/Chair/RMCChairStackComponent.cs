using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.PowerLoader;
using Content.Shared.Physics;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chair;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCChairStackSystem), Other = AccessPermissions.Read)]
public sealed partial class RMCChairStackComponent : Component
{
    [DataField]
    public string ContainerId = "rmc-chair-stack";

    [DataField, AutoNetworkedField]
    public int StackedCount;

    [DataField]
    public string FixtureId = "chair-stack";

    [DataField]
    public IPhysShape FixtureShape = new PhysShapeAabb();

    [DataField]
    public float FixtureDensity = 100;

    [DataField]
    public CollisionGroup CollisionMask = CollisionGroup.FullTileMask | CollisionGroup.MobMask;

    [DataField]
    public CollisionGroup CollisionLayer = CollisionGroup.HighImpassable |
                                           CollisionGroup.MidImpassable |
                                           CollisionGroup.LowImpassable |
                                           CollisionGroup.InteractImpassable |
                                           CollisionGroup.BulletImpassable;

    [DataField]
    public int UnstableThreshold = 8;

    [DataField]
    public float StackCollapseChanceFactor = 50;

    [DataField]
    public float ProjectileCoverage = 0.85f;

    [DataField]
    public float ThrownItemCollapseChance = 0.5f;

    [DataField]
    public float PowerLoaderCollapseChance = 0.5f;

    [DataField]
    public TimeSpan CollisionStun = TimeSpan.FromSeconds(2);

    [DataField]
    public int MinThrowRange = 2;

    [DataField]
    public int MaxThrowRange = 5;

    [DataField]
    public int ScatterDivisor = 2;

    [DataField]
    public float ThrowSpeed = 6.67f;

    [DataField]
    public EntProtoId CollapsedPrototype = "CMChairFolded";

    [DataField]
    public EntProtoId DestructionDrop = "CMSheetMetal1";

    [DataField]
    public ProtoId<ToolQualityPrototype> DismantleQuality = "Anchoring";

    [DataField]
    public EntProtoId<SkillDefinitionComponent> PowerLoaderSkill = "RMCSkillPowerLoader";

    [DataField]
    public EntProtoId PowerLoaderVirtualLeft = "RMCVirtualChairStackLeft";

    [DataField]
    public EntProtoId PowerLoaderVirtualRight = "RMCVirtualChairStackRight";

    [DataField]
    public SoundSpecifier CollapseSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/metal_crash.ogg");

    [DataField]
    public SoundSpecifier DestructionSound = new SoundCollectionSpecifier("MetalBreak");

    [DataField]
    public SoundSpecifier PowerLoaderPickupSound =
        new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_2.ogg");

    [DataField]
    public SoundSpecifier PowerLoaderDropSound =
        new SoundPathSpecifier("/Audio/_RMC14/Machines/hydraulics_1.ogg");

    [ViewVariables]
    public bool Collapsing;
}

[Serializable, NetSerializable]
public enum RMCChairStackVisuals : byte
{
    Count,
}

[RegisterComponent]
public sealed partial class RMCChairStackConstructionBlockerComponent : Component;
