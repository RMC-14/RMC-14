using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCFusionReactorComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Watts = 50_000;

    [DataField, AutoNetworkedField]
    public RMCFusionReactorState State = RMCFusionReactorState.Working;

    [DataField, AutoNetworkedField]
    public string CellContainerSlot = "rmc_fusion_reactor_cell";

    [DataField, AutoNetworkedField]
    public EntProtoId<RMCFusionCellComponent>? StartingCell = "RMCGeneratorFusionCell";

    [DataField, AutoNetworkedField]
    public TimeSpan CellDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan CellRemoveDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(20);

    [DataField, AutoNetworkedField]
    public TimeSpan DestroyDelay = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public float WeldingCost = 1f;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillEngineer";

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> CrowbarQuality = "Prying";

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> WeldingQuality = "Welding";

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> CuttingQuality = "Cutting";

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> WrenchQuality = "Anchoring";

    [DataField, AutoNetworkedField]
    public bool RandomizeDamage;
}

[Serializable, NetSerializable]
public enum RMCFusionReactorState
{
    Working = 0,
    Wrench,
    Wire,
    Weld,
}

[Serializable, NetSerializable]
public enum RMCFusionReactorLayers
{
    Layer,
}

[Serializable, NetSerializable]
public enum RMCFusionReactorVisuals
{
    Off,
    Weld,
    Wire,
    Wrench,
    Ten,
    TwentyFive,
    Fifty,
    SeventyFive,
    Hundred,
    Overloaded,
    Empty,
}
