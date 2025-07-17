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

    [ViewVariables, AutoNetworkedField]
    public TimeSpan LastPainLevelUpdateTime = new(0);

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
    public List<EntityEffect> PainLevels = [];

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "Pain";
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class PainModificator
{
    public TimeSpan Duration;
    public FixedPoint2 EffectStrength;
    public PainModificatorType Type;

    public PainModificator(TimeSpan duration, FixedPoint2 strength, PainModificatorType type)
    {
        Duration = duration;
        EffectStrength = strength;
        Type = type;
    }
}

public enum PainModificatorType : byte
{
    PainReduction,
    PainIncrease,
}
