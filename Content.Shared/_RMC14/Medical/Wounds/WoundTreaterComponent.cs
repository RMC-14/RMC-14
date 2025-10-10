using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Wounds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedWoundsSystem))]
public sealed partial class WoundTreaterComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public WoundType Wound;

    [DataField(required: true), AutoNetworkedField]
    public bool Treats;

    [DataField(required: true), AutoNetworkedField]
    public bool Consumable;

    [DataField(required: true), AutoNetworkedField]
    public ProtoId<DamageGroupPrototype> Group;

    [DataField, AutoNetworkedField]
    public TimeSpan ScalingDoAfter;

    [DataField, AutoNetworkedField]
    public TimeSpan MinimumDoAfter = TimeSpan.FromSeconds(1.25);

    [DataField, AutoNetworkedField]
    public EntProtoId<SkillDefinitionComponent> DoAfterSkill = "RMCSkillMedical";

    [DataField, AutoNetworkedField]
    public float[] DoAfterSkillMultipliers = new[] { 1, 1, 1, 0.75f, 0.5f };

    [DataField, AutoNetworkedField]
    public float SelfTargetDoAfterMultiplier = 1.5f;

    [DataField, AutoNetworkedField]
    public FixedPoint2? Damage;

    [DataField, AutoNetworkedField]
    public FixedPoint2? UnskilledDamage;

    [DataField, AutoNetworkedField]
    public bool CanUseUnskilled;

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> Skills = new();

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TreatBeginSound;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? TreatEndSound;

    [DataField, AutoNetworkedField]
    public LocId? UserPopup;

    [DataField, AutoNetworkedField]
    public LocId? TargetPopup;

    [DataField, AutoNetworkedField]
    public LocId? OthersPopup;

    [DataField, AutoNetworkedField]
    public LocId? TargetStartPopup;

    [DataField, AutoNetworkedField]
    public LocId? UserFinishPopup;

    [DataField, AutoNetworkedField]
    public LocId? TargetFinishPopup;

    [DataField, AutoNetworkedField]
    public LocId? OthersFinishPopup;

    [DataField, AutoNetworkedField]
    public LocId? NoneSelfPopup;

    [DataField, AutoNetworkedField]
    public LocId? NoneOtherPopup;

    [DataField, AutoNetworkedField]
    public LocId? NoWoundsOnUserPopup;

    [DataField, AutoNetworkedField]
    public LocId? NoWoundsOnTargetPopup;
}
