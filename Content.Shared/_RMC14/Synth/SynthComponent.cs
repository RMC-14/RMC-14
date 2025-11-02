using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared._RMC14.Medical.IV;
using Content.Shared._RMC14.Medical.Surgery;
using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared._RMC14.NightVision;
using Content.Shared._RMC14.Suicide;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body.Organ;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.ForceSay;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Electrocution;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.StatusIcon;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Synth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSynthSystem))]
public sealed partial class SynthComponent : Component
{
    [DataField]
    public ComponentRegistry? RemoveComponents;

    [DataField]
    public ComponentRegistry AlwaysAddComponents = new()
    {
        {"NightVision", new(new NightVisionComponent()
        {
          Innate = true,
          State = NightVisionState.Half,
          OnlyHalf = true,
          Alert = "SynthNightVision",
        }, [])},
        {"EyeProtection", new(new EyeProtectionComponent(), [])},
        {"WoundableUntreatable", new(new WoundableUntreatableComponent(), [])},
        {"Insulated", new(new InsulatedComponent(), [])},
        {"RMCLeapProtection", new(new RMCLeapProtectionComponent()
        {
            InherentStunDuration = TimeSpan.FromSeconds(3),
        }, [])},
    };

    [DataField]
    public ComponentRegistry AlwaysRemoveComponents = new()
    {
        {"CMSurgeryTarget", new(new CMSurgeryTargetComponent(), [])},
        {"Infectable", new(new InfectableComponent(), [])},
        {"Hunger", new(new HungerComponent(), [])},
        {"Thirst", new(new ThirstComponent(), [])},
        {"Perishable", new(new PerishableComponent(), [])},
        {"Stamina", new(new StaminaComponent(), [])},
        // ThermalRegulator is sadly a server only component from upstream,
        // so we have to keep it in RemoveComponents.
        {"DamageForceSay", new(new DamageForceSayComponent(), [])},
        {"Dna", new(new DnaComponent(), [])},
        {"InjectableSolution", new(new InjectableSolutionComponent(), [])},
        {"IVDripTarget", new(new IVDripTargetComponent(), [])},
        {"RMCSuicide", new(new RMCSuicideComponent(), [])},
    };

    /// <summary>
    /// The final stun duration (after endurance skill) is divided by this number.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? StunResistance = 2.5f;

    [DataField, AutoNetworkedField]
    public bool CanUseGuns = false;

    [DataField, AutoNetworkedField]
    public bool CanUseMeleeWeapons = true;

    /// <summary>
    /// The blood reagent to give the synth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype> NewBloodReagent = "RMCSynthBlood";

    /// <summary>
    /// The damage modifier set to give the synth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<DamageModifierSetPrototype> NewDamageModifier = "RMCSynth";

    [DataField, AutoNetworkedField]
    public LocId SpeciesName = "rmc-species-name-synth";

    /// <summary>
    /// I.E. 1st generation, 3rd generation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId Generation = "rmc-species-synth-generation-third";

    [DataField, AutoNetworkedField]
    public LocId FixedIdentityReplacement = "cm-chatsan-replacement-synth";

    [DataField, AutoNetworkedField]
    public Dictionary<RMCHealthIconTypes, ProtoId<HealthIconPrototype>> HealthIconOverrides = new()
    {
        [RMCHealthIconTypes.Healthy] = "RMCHealthIconHealthySynth",
        [RMCHealthIconTypes.DeadDefib] = "RMCHealthIconDeadSynth",
        [RMCHealthIconTypes.DeadClose] = "RMCHealthIconDeadSynth",
        [RMCHealthIconTypes.DeadAlmost] = "RMCHealthIconDeadSynth",
        [RMCHealthIconTypes.DeadDNR] = "RMCHealthIconDeadDNRSynth",
        [RMCHealthIconTypes.Dead] = "RMCHealthIconDeadSynth",
        [RMCHealthIconTypes.HCDead] = "RMCHealthIconDeadSynth",
    };

    /// <summary>
    /// New brain organ to add when the synth is created.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<OrganComponent> NewBrain = "RMCOrganSynthBrain";

    /// <summary>
    /// The time it takes to repair the synth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RepairTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// The time it takes to repair the synth, if you are the synth.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan SelfRepairTime = TimeSpan.FromSeconds(30);

    [DataField, AutoNetworkedField]
    public FixedPoint2 CritThreshold = FixedPoint2.New(199);

    /// <summary>
    /// The tool quality needed to repair the synth brute damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> RepairQuality = "Welding";

    [DataField]
    public DamageSpecifier? WelderDamageToRepair = new()
    {
        DamageDict = {
            ["Blunt"] = -15,
            ["Piercing"] = -15,
            ["Slash"] = -15,
        },
    };

    [DataField]
    public DamageSpecifier? CableCoilDamageToRepair = new()
    {
        DamageDict = {
            ["Caustic"] = -15,
            ["Heat"] = -15,
            ["Shock"] = -15,
            ["Cold"] = -15,
        },
    };

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> WelderDamageGroup = "Brute";

    [DataField, AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> CableCoilDamageGroup = "Burn";

    [DataField, AutoNetworkedField]
    public string DamageVisualsColor = "#EEEEEE";
}
