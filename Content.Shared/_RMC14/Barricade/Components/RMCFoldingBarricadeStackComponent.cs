using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Barricade;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCFoldingBarricadeSystem))]
public sealed partial class RMCFoldingBarricadeStackComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId DeployedPrototype = "CMBarricadeFolding";

    [DataField, AutoNetworkedField]
    public float MaxDamage = 350;

    [DataField, AutoNetworkedField]
    public List<float> StoredDamage = new();

    [DataField, AutoNetworkedField]
    public TimeSpan DeployDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public float RepairAmount = 200;

    [DataField, AutoNetworkedField]
    public FixedPoint2 RepairFuel = FixedPoint2.New(2);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> RepairSkill = "RMCSkillConstruction";

    [DataField, AutoNetworkedField]
    public float RepairDelayPerDamaged = 10;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? RepairSound = new SoundPathSpecifier("/Audio/Items/welder.ogg");

    public bool SuppressCountChange;

    public List<float> PendingSplitDamage = new();
}
