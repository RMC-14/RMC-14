using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.TacticalMap;
using System.Collections.Generic;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Sensor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SensorTowerSystem))]
public sealed partial class SensorTowerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillEngineer";

    [DataField, AutoNetworkedField]
    public int SkillLevel = 2;

    [DataField, AutoNetworkedField]
    public SensorTowerState State = SensorTowerState.Weld;

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> WeldingQuality = "Welding";

    [DataField, AutoNetworkedField]
    public TimeSpan WeldingDelay = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public float WeldingCost = 1f;

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> CuttingQuality = "Cutting";

    [DataField, AutoNetworkedField]
    public TimeSpan CuttingDelay = TimeSpan.FromSeconds(12);

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> WrenchQuality = "Anchoring";

    [DataField, AutoNetworkedField]
    public TimeSpan WrenchDelay = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public float BreakChance = 0.15f;

    [DataField, AutoNetworkedField]
    public TimeSpan BreakEvery = TimeSpan.FromSeconds(175);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBreakAt;

    [DataField, AutoNetworkedField]
    public TimeSpan DestroyDelay = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public List<ProtoId<TacticalMapLayerPrototype>> RevealForLayers = new();

    [DataField, AutoNetworkedField]
    public List<ProtoId<TacticalMapLayerPrototype>> RevealLayers = new();
}

[Serializable, NetSerializable]
public enum SensorTowerState
{
    Weld = 0,
    Wire,
    Wrench,
    Off,
    On,
}

[Serializable, NetSerializable]
public enum SensorTowerLayers
{
    Layer,
}
