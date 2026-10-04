using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Syringe;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSyringeComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool AllowInstantSelfInject = true;

    [DataField, AutoNetworkedField]
    public bool AllowInstantDraw = true;

    [DataField, AutoNetworkedField]
    public bool AllowBloodDraw = true;

    [DataField, AutoNetworkedField]
    public bool NoDrawOnAliveHostiles = true;

    [DataField, AutoNetworkedField]
    public bool SkillBasedDelay = true;

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> SkillCheck = "RMCSkillMedical";

    // Combat inject
    [DataField, AutoNetworkedField]
    public int MinArmorBlock = 5;

    [DataField, AutoNetworkedField]
    public float ArmorFailChance = 0.5f;

    [DataField, AutoNetworkedField]
    public SoundSpecifier ArmorSound = new SoundCollectionSpecifier("RMCShieldImpact", AudioParams.Default.WithVolume(-6));

    [DataField, AutoNetworkedField]
    public DamageSpecifier InjectDamage;

    [DataField, AutoNetworkedField]
    public int CombatInjectPenalty = 5;

    [DataField, AutoNetworkedField]
    public EntProtoId BrokenSyringe = "RMCSyringeBroken";

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> CQCSkill = "RMCSkillCqc";

    [DataField, AutoNetworkedField]
    public int CQCMinFailLevel = 2;

    [DataField, AutoNetworkedField]
    public TimeSpan CQCKnockdown = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public SoundSpecifier CQCSuccessSound = new SoundPathSpecifier("/Audio/Effects/thudswoosh.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier BreakSound = new SoundPathSpecifier("/Audio/Items/bottle_clunk_2.ogg");
}

[ByRefEvent]
public record struct RMCSyringeGetDelayEvent(TimeSpan Delay, InjectorToggleMode Mode, EntityUid User, EntityUid Target, bool Cancelled = false);
