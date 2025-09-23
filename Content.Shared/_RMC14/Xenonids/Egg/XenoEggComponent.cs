using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Egg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class XenoEggComponent : Component
{
    [DataField, AutoNetworkedField]
    public XenoEggState State;

    [DataField, AutoNetworkedField]
    public TimeSpan MinTime = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan MaxTime = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public TimeSpan EggOpenTime = TimeSpan.FromSeconds(0.9);

    [DataField, AutoNetworkedField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? GrowAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? OpenAt;

    [DataField, AutoNetworkedField]
    public string ItemState = "egg_item";

    [DataField, AutoNetworkedField]
    public string GrowingState = "egg_growing";

    [DataField, AutoNetworkedField]
    public string GrownState = "egg";

    [DataField, AutoNetworkedField]
    public string OpenedState = "egg_opened";

    [DataField, AutoNetworkedField]
    public string OpeningState = "egg_opening";

    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "CMXenoParasite";

    [DataField]
    public string NormalSprite = "_RMC14/Structures/Xenos/xeno_egg.rsi";

    [DataField]
    public string FragileSprite = "_RMC14/Structures/Xenos/xeno_egg_fragile.rsi";

    [DataField]
    public string SustainedSprite = "_RMC14/Structures/Xenos/xeno_egg_fragile_eggsac.rsi";

    [DataField, AutoNetworkedField]
    public string CurrentSprite = "_RMC14/Structures/Xenos/xeno_egg.rsi";

    [DataField, AutoNetworkedField]
    public TimeSpan CheckWeedsAt;

    [DataField]
    public TimeSpan CheckWeedsDelay = TimeSpan.FromSeconds(2); //To not check constantly for weeds

    [DataField]
    public TimeSpan FragileEggDuration = TimeSpan.FromMinutes(6);

    /// <summary>
    ///     The container ID of where the creature is stored in the egg.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string CreatureContainerId = "rmc_egg_container";

    /// <summary>
    ///     The uid of the creatire that spawned from the egg.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SpawnedCreature;

    /// <summary>
    ///     The uid of the creatire that triggered the egg.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? InfectTarget;

    /// <summary>
    ///     The ent to spawn on normal destruction.
    /// </summary>
    [DataField]
    public EntProtoId EggDestroyed = "XenoEggDestroyed";

    /// <summary>
    ///     The ent to spawn on fragile destruction.
    /// </summary>
    [DataField]
    public EntProtoId EggDestroyedFragile = "XenoEggDestroyedFragile";

    /// <summary>
    ///     The ent to spawn on sustained destruction.
    /// </summary>
    [DataField]
    public EntProtoId EggDestroyedSustained = "XenoEggDestroyedFragileSustained";

    /// <summary>
    ///     How long the creature jitters for when it exits the egg.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CreatureExitEggJitterDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public SoundSpecifier BurstSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_egg_burst.ogg");

    public SoundSpecifier PlantSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_egg_move.ogg");

    [DataField, AutoNetworkedField]
    public string GrowingLayerFixture = "fix1";

    [DataField, AutoNetworkedField]
    public CollisionGroup GrowingLayer = CollisionGroup.BulletImpassable;

    [DataField, AutoNetworkedField]
    public string GrowingMaskFixture = "xeno_egg";

    [DataField, AutoNetworkedField]
    public IPhysShape GrowingMaskShape = new PhysShapeCircle(1.5f);

    [DataField, AutoNetworkedField]
    public CollisionGroup GrowingMask = CollisionGroup.MobLayer;

    [DataField, AutoNetworkedField]
    public bool GrownFixtures;
}

[Serializable, NetSerializable]
public enum XenoEggState
{
    Item,
    Growing,
    Grown,
    Opening,
    Opened,
    Fragile,
    Sustained
}

[Serializable, NetSerializable]
public enum XenoEggLayers
{
    Base
}
