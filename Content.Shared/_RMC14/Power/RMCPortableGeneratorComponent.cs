using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Power;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRMCPowerSystem))]
public sealed partial class RMCPortableGeneratorComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Watts = 5000;

    [DataField, AutoNetworkedField]
    public bool On;

    [DataField, AutoNetworkedField]
    public int PowerGenPercent = 100;

    [DataField, AutoNetworkedField]
    public int MinPowerPercent = 100;

    [DataField, AutoNetworkedField]
    public int MaxPowerPercent = 400;

    [DataField, AutoNetworkedField]
    public int PowerPercentStep = 100;

    [DataField, AutoNetworkedField]
    public int Sheets;

    [DataField, AutoNetworkedField]
    public int MaxSheets = 100;

    [DataField, AutoNetworkedField]
    public float SheetFraction;

    [DataField, AutoNetworkedField]
    public float TimePerSheet = 70f;

    [DataField, AutoNetworkedField]
    public string FuelStackType = "CMPhoron";

    [DataField, AutoNetworkedField]
    public string FuelEntity = "CMSheetPhoron1";

    [DataField, AutoNetworkedField]
    public string FuelName = "phoron";

    [DataField, AutoNetworkedField]
    public float Heat;

    [DataField, AutoNetworkedField]
    public float OverheatThreshold = 300f;

    [DataField, AutoNetworkedField]
    public bool CritFail;

    [DataField, AutoNetworkedField]
    public float ExplosionIntensity = 50f;

    [DataField, AutoNetworkedField]
    public float ExplosionSlope = 5f;

    [DataField, AutoNetworkedField]
    public float ExplosionMaxIntensity = 10f;

    [DataField, AutoNetworkedField]
    public TimeSpan StartDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> Skill = "RMCSkillEngineer";

}

[Serializable, NetSerializable]
public enum RMCPortableGeneratorUiKey
{
    Key,
}

[Serializable, NetSerializable]
public enum RMCPortableGeneratorVisuals
{
    Running,
}

[Serializable, NetSerializable]
public enum RMCPortableGeneratorVisualLayers
{
    Base,
    Unlit,
}

[Serializable, NetSerializable]
public sealed class RMCPortableGeneratorToggleBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCPortableGeneratorEjectFuelBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCPortableGeneratorRaisePowerBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCPortableGeneratorLowerPowerBuiMsg : BoundUserInterfaceMessage;
