using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;
using Content.Shared.EntityEffects;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.Pain;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class PainComponent : Component
{
    /// <summary>
    /// Pain value derived from overall damage to the body without modificators.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 CurrentPain = FixedPoint2.Zero;

    /// <summary>
    /// Pain value with all modificators and limited to 100.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 CurrentPainPercentage = FixedPoint2.Zero;

    [ViewVariables, AutoNetworkedField]
    public int CurrentPainLevel = 0;

    [DataField, AutoNetworkedField]
    public TimeSpan PainLevelUpdateRate = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextPainLevelUpdateTime = new(0);

    [DataField, AutoNetworkedField]
    public TimeSpan EffectUpdateRate = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextEffectUpdateTime = new(0);

    [ViewVariables, Access(typeof(PainSystem)), AutoNetworkedField]
    public List<PainModificator> PainModificators = [];

    [DataField, AutoNetworkedField]
    public FixedPoint2 PainReductionDecreaceRate = FixedPoint2.New(0.25);

    [DataField, AutoNetworkedField]
    public FixedPoint2 BrutePainMultiplier = FixedPoint2.New(1);

    [DataField, AutoNetworkedField]
    public FixedPoint2 BurnPainMultiplier = FixedPoint2.New(1.2);

    [DataField, AutoNetworkedField]
    public FixedPoint2 ToxinPainMultiplier = FixedPoint2.New(1.5);

    [DataField, AutoNetworkedField]
    public FixedPoint2 AirlossPainMultiplier = FixedPoint2.Zero;

    [DataField(required: true, serverOnly: true)]
    public List<PainLevel> PainLevels = [];

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "HumanoidPainHealth";
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class PainModificator
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ExpireAt;
    public FixedPoint2 EffectStrength;
    public PainModificatorType Type;

    public PainModificator(TimeSpan expireAt, FixedPoint2 strength, PainModificatorType type)
    {
        ExpireAt = expireAt;
        EffectStrength = strength;
        Type = type;
    }
}

[DataRecord]
public record struct PainLevel(FixedPoint2 Threshold, List<EntityEffect> LevelEffects);

public enum PainModificatorType : byte
{
    PainReduction,
    PainIncrease,
}
