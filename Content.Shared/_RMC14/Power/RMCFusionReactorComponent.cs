using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Tools;
using Robust.Shared.Audio;
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
    public bool Overloaded;

    [DataField, AutoNetworkedField]
    public TimeSpan OverloadDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> OverloadSkill = "RMCSkillEngineer";

    [DataField, AutoNetworkedField]
    public int OverloadSkillLevel = 2;

    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> OverloadQuality = "Pulsing";

    [DataField]
    public TimeSpan OverloadFeedbackMinDelay = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan OverloadFeedbackMaxDelay = TimeSpan.FromSeconds(35);

    [ViewVariables]
    public TimeSpan OverloadNextFeedbackAt;

    [DataField]
    public SoundSpecifier OverloadHumSound = new SoundPathSpecifier(
        "/Audio/Ambience/Objects/engine_hum.ogg",
        AudioParams.Default.WithVolume(0f).WithMaxDistance(10f).WithVariation(0.05f));

    [DataField]
    public SoundSpecifier OverloadHissSound = new SoundPathSpecifier(
        "/Audio/Ambience/Objects/gas_hiss.ogg",
        AudioParams.Default.WithVolume(2.1f).WithMaxDistance(10f).WithVariation(0.05f));

    [DataField]
    public SoundSpecifier OverloadStopSound = new SoundPathSpecifier(
        "/Audio/_RMC14/Structures/metalhit.ogg",
        AudioParams.Default.WithVolume(-2f).WithMaxDistance(10f).WithVariation(0.05f));

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
