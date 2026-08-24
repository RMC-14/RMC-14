using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Stun;

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
    ///     DM value to seconds for calc, NOT actual duration
    ///     See RMCSizeStunSystem for calc if true
    /// </summary>
    [DataField]
    public bool LosesEffectWithRange;

    [DataField]
    public bool SlowsEffectBigXenos;

    /// <summary>
    ///     DM value to seconds for calc, NOT actual duration
    ///     Multiply CM's apply_effect/proc(value) by 2 for this field
    ///     Xeno stun duration = StunTime x 0.667
    /// </summary>
    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1.4);

    /// <summary>
    ///     DM value to seconds for calc, NOT actual duration
    ///     Multiply CM's apply_effect/proc(value) by 2
    /// </summary>
    [DataField]
    public TimeSpan SuperSlowTime = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     DM value to seconds for calc, NOT actual duration
    ///     Multiply CM's apply_effect/proc(value) by 2
    /// </summary>
    [DataField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(2);

    /// <summary>
    ///     DM value to seconds for calc, NOT actual duration
    ///     Multiply CM's apply_effect/proc(value) by 2
    /// </summary>
    [DataField]
    public TimeSpan DazeTime;

    [DataField]
    public float StunArea = 0.5f;
}
