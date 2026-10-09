using Robust.Shared.GameStates;
using Content.Shared.FixedPoint;
using Content.Shared.EntityEffects;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage.Prototypes;

namespace Content.Shared._RMC14.Medical.Pain;

[Access(typeof(PainSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true), AutoGenerateComponentPause]
public sealed partial class PainComponent : Component
{
    /// <summary>
    /// Base pain value derived from overall body damage multiplied by <see cref="DamageGroupPainMultipliers"/> per damage group,
    /// without accounting for any <see cref="PainModifiers"/>.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 BasePain = FixedPoint2.Zero;

    /// <summary>
    /// 0 to 100 value representing how much pain the player actually <i>feels</i> after applying any <see cref="PainModifiers"/>
    /// like painkillers to <see cref="BasePain"/>.
    /// </summary>
    /// <remarks>
    /// This is used to select the highest <see cref="PainLevel"/> in <see cref="PainLevels"/> where <c>PainLevel.Threshold &lt;= PerceivedPain</c>.
    /// <para>
    /// Please use <see cref="PainSystem.SetPerceivedPain(Entity{PainComponent}, FixedPoint2)"/> when setting this.
    /// </para>
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 PerceivedPain = FixedPoint2.Zero;

    /// <summary>
    /// Zero-based index of the currently active <see cref="PainLevel"/> in the <see cref="PainLevels"/> list.
    /// This is set based on the highest <see cref="PainLevel.Threshold"/> passed by <see cref="PerceivedPain"/>.
    /// </summary>
    /// <remarks>
    /// Please use <see cref="PainSystem.SetCurrentPainLevelIdx(Entity{PainComponent}, int)"/> when setting this.
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public int CurrentPainLevelIdx = 0;

    /// <summary>
    /// List of currently active <see cref="PainModifier"/>s, either increasing or decreasing the amount
    /// of pain felt by the player in <see cref="PerceivedPain"/>.
    /// </summary>
    /// <remarks>
    /// Due to painkiller <see cref="EntityEffect"/>s (seemingly) not being predictable, the values of any <see cref="PainModifier"/>s
    /// caused by them may be out of sync on the Client's side. <see cref="PainModifier.ExpireAt"/> in particular. <br/>
    /// This is all handled already so doesn't cause any problems, it's just worth noting.
    /// </remarks>
    /// <seealso cref="PainSystem.UpdatePerceivedPain(Entity{PainComponent})"/>
    [ViewVariables, AutoNetworkedField]
    public List<PainModifier> PainModifiers = [];

    /// <summary>
    /// List of <see cref="PainLevel"/>s structs, each containing its own list of <see cref="EntityEffect"/>s to be triggered when
    /// <see cref="PerceivedPain"/> passes their <see cref="PainLevel.Threshold"/>.<br/>
    /// Only one <see cref="PainLevel"/> can be active at a time, with the currently active level indicated by its index in <see cref="CurrentPainLevelIdx"/>.
    /// </summary>
    /// <remarks>
    /// When creating a new <c>PainLevels</c> list in a .yml file or otherwise, each level in the list must be positioned in order of their <c>Threshold</c> values,
    /// and the lowest level must have a <c>Threshold</c> of 0 for a base "no pain" state,
    /// </remarks>
    [DataField(required: true)]
    public List<PainLevel> PainLevels = [];

    /// <summary>
    /// Dictionary of <see cref="DamageGroupPrototype"/>s and a multiplier for the <see cref="BasePain"/> amount caused by each group when inflicted.<br/>
    /// 0 == No pain | 1 == 1:1 damage to pain | 1.5 == 50% increase | etc.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<DamageGroupPrototype>, FixedPoint2> DamageGroupPainMultipliers = new()
    {
        {"Brute", FixedPoint2.New(1)},
        {"Burn", FixedPoint2.New(1.2)},
        {"Toxin", FixedPoint2.New(1.5)},
        {"Airloss", FixedPoint2.Zero}
    };

    /// <summary>
    /// Controls the rate at which <see cref="PainModifierType.PainReduction"/> <see cref="PainModifier"/>s lose effectiveness as <see cref="BasePain"/> increases.
    /// </summary>
    /// <remarks>
    /// The actual calculation can be seen in <see cref="PainSystem.UpdatePerceivedPain(Entity{PainComponent})"/>, but it essentially works out as:<br/>
    /// "For every point of (<see cref="BasePain"/> + Increase modifiers), reduction strength decreases by <see cref="PainReductionDecreaseRate"/>".
    /// </remarks>
    [DataField, AutoNetworkedField]
    public FixedPoint2 PainReductionDecreaseRate = FixedPoint2.New(0.25);

    /// <summary>
    /// Time between each update of this component in <see cref="PainSystem.Update(float)"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextUpdateTime = new(0);

    /// <summary>
    /// Time between each update of <see cref="CurrentPainLevelIdx"/>.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PainLevelUpdateRate = TimeSpan.FromSeconds(2);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan NextPainLevelUpdateTime = new(0);

    /// <summary>
    /// Prototype ID of the health alert icon on the right side of the player's screen.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "HumanoidPainHealth";

    /// <summary>
    /// The previous <see cref="PerceivedPain"/>, used so that <see cref="PainSystem.OnPainState(Entity{PainComponent}, ref AfterAutoHandleStateEvent)"/>
    /// can check if the value has changed between state updates.
    /// </summary>
    /// <remarks>
    /// This field is specifically <i>not</i> networked, and should be client-side only.
    /// </remarks>
    /// <seealso cref="PreviousPainLevelIdx"/>
    public FixedPoint2 PreviousPerceivedPain;

    /// <summary>
    /// The previous <see cref="CurrentPainLevelIdx"/>, used so that <see cref="PainSystem.OnPainState(Entity{PainComponent}, ref AfterAutoHandleStateEvent)"/>
    /// can check if the value has changed between state updates.
    /// </summary>
    /// <remarks>
    /// This field is specifically <i>not</i> networked, and should be client-side only.
    /// </remarks>
    /// <seealso cref="PreviousPerceivedPain"/>
    public int PreviousPainLevelIdx;
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
public sealed partial class PainModifier : IEquatable<PainModifier>
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

    public bool Equals(PainModifier? other)
    {
        return other is not null &&
            ExpireAt == other.ExpireAt &&
            EffectStrength == other.EffectStrength &&
            Type == other.Type;
    }

    public override bool Equals(object? obj) => Equals(obj as PainModifier);
    public override int GetHashCode() => HashCode.Combine(ExpireAt, EffectStrength, Type);
}

[Serializable, NetSerializable]
public enum PainModifierType : byte
{
    PainReduction,
    PainIncrease,
}
