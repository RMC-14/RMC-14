﻿using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Audio;

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
    ///     How long the creature jitters for when it exits the egg.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CreatureExitEggJitterDuration = TimeSpan.FromSeconds(6);

    public SoundSpecifier PlantSound = new SoundPathSpecifier("/Audio/Effects/Fluids/splat.ogg");

    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_egg_move.ogg");
}

[Serializable, NetSerializable]
public enum XenoEggState
{
    Item,
    Growing,
    Grown,
    Opening,
    Opened
}

[Serializable, NetSerializable]
public enum XenoEggLayers
{
    Base
}
