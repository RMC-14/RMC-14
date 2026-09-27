using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.PillBottle;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCBreakablePillBottleSystem))]
public sealed partial class RMCBreakablePillBottleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Broken;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? BreakWhitelist;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? RepairWhitelist;

    [DataField, AutoNetworkedField]
    public TimeSpan BreakDelay = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public int SpillAmount = 3;

    [DataField, AutoNetworkedField]
    public int SpillMaxDistance = 3;

    [DataField, AutoNetworkedField]
    public float SpillStepChance = 0.35f;

    [DataField, AutoNetworkedField]
    public float SpillScatter = 0.4f;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> RepairSkill = "RMCSkillSurgery";

    [DataField, AutoNetworkedField]
    public List<TimeSpan> RepairDelays = new() { TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(2), TimeSpan.Zero };

    [DataField, AutoNetworkedField]
    public string BrokenState = "broken";

    [DataField, AutoNetworkedField]
    public List<string> HiddenLayersWhenBroken = new() { "openLayer", "closedLayer" };

    [DataField, AutoNetworkedField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/pop.ogg", AudioParams.Default.WithVariation(0.05f));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? BreakSound = new SoundCollectionSpecifier("CMPillBottleOpen", AudioParams.Default.WithVariation(0.05f));

    [DataField, AutoNetworkedField]
    public SoundSpecifier? SpillSound = new SoundPathSpecifier("/Audio/_RMC14/Medical/pill_spill.ogg", AudioParams.Default.WithVariation(0.05f));
}
