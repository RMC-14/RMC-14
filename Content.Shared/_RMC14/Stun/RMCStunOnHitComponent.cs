using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Stun;
/// <summary>
///    Duration values here DO NOT represent the actual in game duration, PURELY CALC HERE.
///    E.g If CM apply_effect(2, STUN) would mean you put StunTime: 4 in the yml
///    Typically the duration values scaled further by RMCStatusEffectSystem, RMCSlowdownModifier or RMCSizeStunSystem.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSizeStunSystem))]
public sealed partial class RMCStunOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public MapCoordinates? ShotFrom;

    [DataField, AutoNetworkedField]
    public List<RMCStunOnHit> Stuns = new();
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct RMCStunOnHit()
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public float MaxRange = 2.5f;

    [DataField]
    public float KnockBackPowerMin = 1;

    [DataField]
    public float KnockBackPowerMax = 1;

    [DataField]
    public float KnockBackSpeed = 5;

    [DataField]
    public bool ForceKnockBack;

    /// <summary>
    ///     See RMCSizeStunSystem for calc if true
    /// </summary>
    [DataField]
    public bool LosesEffectWithRange;

    [DataField]
    public bool SlowsEffectBigXenos;

    /// <summary>
    ///     NOT actual duration.
    ///     This value is CM's apply_effect(value) x 2
    ///     Actual duration = StunTime x the appropriate factor in RMCStatusEffectSystem.
    /// </summary>
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1.4);

    /// <summary>
    ///     NOT actual duration.
    ///     This value is CM's apply_effect(value) x 2
    ///     Actual duration scaled by RMCSpeciesSlowdownModifier. Check the base ymls
    /// </summary>
    [DataField]
    public TimeSpan SuperSlowTime = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     NOT actual duration.
    ///     This value is CM's apply_effect(value) x 2
    ///     Actual duration scaled by RMCSpeciesSlowdownModifier. Check the base ymls
    /// </summary>
    [DataField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     NOT actual duration.
    ///     This value is CM's apply_effect(value) x 2
    ///     Actual duration scaled by RMCSizeStunSystem and RMCStatusEffectSystem.
    /// </summary>
    [DataField]
    public TimeSpan DazeTime;

    [DataField]
    public float StunArea = 0.5f;
}
