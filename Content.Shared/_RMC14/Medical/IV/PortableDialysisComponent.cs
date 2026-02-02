using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PortableDialysisComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 ReagentRemovalAmount = FixedPoint2.New(1.5);

    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodRemovalCost = FixedPoint2.New(6);

    [DataField, AutoNetworkedField]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan TransferAt;

    [DataField, AutoNetworkedField]
    public EntityUid? AttachedTo;

    [DataField, AutoNetworkedField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(1.2);

    [DataField, AutoNetworkedField]
    public int Range = 2;

    [DataField]
    public DamageSpecifier? RipDamage;

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> RipEmote = "Scream";

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> SkillRequired = new() { ["RMCSkillMedical"] = 2 };

    [DataField, AutoNetworkedField]
    public ProtoId<ReagentPrototype>[] NonTransferableReagents = ["Blood"];

    [DataField, AutoNetworkedField]
    public bool IsAttaching;

    [DataField, AutoNetworkedField]
    public bool IsDetaching;
}

[Serializable, NetSerializable]
public enum DialysisVisualLayers
{
    Attachment,
    Effect,
    Filtering,
    Battery
}

[Serializable, NetSerializable]
public enum DialysisBatteryLevel : byte
{
    Empty,
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh,
    Full
}

[Serializable, NetSerializable]
public enum DialysisVisuals : byte
{
    BatteryLevel
}
