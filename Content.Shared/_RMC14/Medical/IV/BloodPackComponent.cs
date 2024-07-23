using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class BloodPackComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Solution = "pack";

    [DataField, AutoNetworkedField]
    public FixedPoint2 FillPercentage;

    [DataField, AutoNetworkedField]
    public Color FillColor;

    [DataField, AutoNetworkedField]
    public int MaxFillLevels = 7;

    [DataField, AutoNetworkedField]
    public string FillBaseName = "bloodpack";

    [DataField, AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    [DataField, AutoNetworkedField]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan TransferAt;

    [DataField, AutoNetworkedField]
    public EntityUid? AttachedTo;

    [DataField, AutoNetworkedField]
    public TimeSpan AttachDelay = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public int Range = 2;

    [DataField]
    public DamageSpecifier? RipDamage;

    [DataField, AutoNetworkedField]
    public bool Injecting = true;

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> RipEmote = "Scream";

    [DataField, AutoNetworkedField]
    public Skills SkillRequired = new() { Surgery = 1 };

	// TODO RMC-14 blood types
	[DataField, AutoNetworkedField]
	public string[] TransferableReagents = ["Blood"];
}

[Serializable, NetSerializable]
public enum BloodPackVisuals
{
    Label,
    Fill
}
