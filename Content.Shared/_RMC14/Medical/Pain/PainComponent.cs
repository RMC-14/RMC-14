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
    /// Base pain value derived from overall damage to the body, without accounting for any <see cref="PainModifiers"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 CurrentPain = FixedPoint2.Zero;

    /// <summary>
    /// 0 to 100 value representing how much pain the player actually <i>feels</i> after applying any <see cref="PainModifiers"/> like painkillers.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 CurrentPainPercentage = FixedPoint2.Zero;

    /// <summary>
    /// Zero-based index of the currently active <see cref="PainLevel"/> in the <see cref="PainLevels"/> list.
    /// This is set based on the highest <see cref="PainLevel.Threshold"/> passed by <see cref="CurrentPainPercentage"/>.
    /// </summary>
    /// <remarks>
    /// Please use <see cref="PainSystem.SetCurrentPainLevel(Entity{PainComponent}, int)"/> when setting this
    /// so that the user's pain overlay can be updated with <see cref="PainLevelChangedEvent"/>.
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public int CurrentPainLevel = 0;

    /// <summary>
    /// List of currently active <see cref="PainModifier"/>s, either increasing or decreasing the amount
    /// of pain felt by the player in <see cref="CurrentPainPercentage"/>.
    /// </summary>
    /// <remarks>
    /// Due to painkiller <see cref="EntityEffect"/>s not being predictable, the values of any <see cref="PainModifier"/>s
    /// caused by them may be out of sync on the Client's side. <see cref="PainModifier.ExpireAt"/> in particular.
    /// </remarks>
    /// <seealso cref="PainSystem.UpdateCurrentPainPercentage(Entity{PainComponent})"/>
    [ViewVariables, Access(typeof(PainSystem)), AutoNetworkedField]
    public List<PainModifier> PainModifiers = [];

    /// <summary>
    /// Time between each update of this component in <see cref="PainSystem.Update(float)"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdateTime = new(0);

    /// <summary>
    /// Time between each update of <see cref="CurrentPainLevel"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PainLevelUpdateRate = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextPainLevelUpdateTime = new(0);

    [DataField, AutoNetworkedField]
    public FixedPoint2 PainReductionDecreaseRate = FixedPoint2.New(0.25);

    [DataField, AutoNetworkedField]
    public FixedPoint2 BrutePainMultiplier = FixedPoint2.New(1);

    [DataField, AutoNetworkedField]
    public FixedPoint2 BurnPainMultiplier = FixedPoint2.New(1.2);

    [DataField, AutoNetworkedField]
    public FixedPoint2 ToxinPainMultiplier = FixedPoint2.New(1.5);

    [DataField, AutoNetworkedField]
    public FixedPoint2 AirlossPainMultiplier = FixedPoint2.Zero;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "HumanoidPainHealth";

    /// <summary>
    /// List of <see cref="PainLevel"/>s structs, each containing its own list of <see cref="EntityEffect"/>s to be triggered when
    /// <see cref="CurrentPainPercentage"/> passes their <see cref="PainLevel.Threshold"/>.<br/>
    /// Only one <see cref="PainLevel"/> can be active at a time, with the currently active level indicated by its index in <see cref="CurrentPainLevel"/>.
    /// </summary>
    [DataField(readOnly: true, required: true)]
    public List<PainLevel> PainLevels = [];
}

[DataRecord]
public record struct PainLevel
{
    [DataField]
    public FixedPoint2 Threshold;

    // Server-side only due to `EntityEffect` not being serializable. (`Threshold` is fine though)
    [DataField(serverOnly: true)]
    public List<EntityEffect> LevelEffects;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class PainModifier
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan ExpireAt;

    [DataField]
    public FixedPoint2 EffectStrength;

    [DataField]
    public PainModifierType Type;

    public PainModifier(TimeSpan expireAt, FixedPoint2 strength, PainModifierType type)
    {
        ExpireAt = expireAt;
        EffectStrength = strength;
        Type = type;
    }
}

[Serializable, NetSerializable]
public enum PainModifierType : byte
{
    PainReduction,
    PainIncrease,
}
