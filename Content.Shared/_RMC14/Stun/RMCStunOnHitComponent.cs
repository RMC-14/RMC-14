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

    [DataField]
    public bool LosesEffectWithRange;

    [DataField]
    public bool SlowsEffectBigXenos;

    [DataField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1.4);

    [DataField]
    public TimeSpan SuperSlowTime = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan DazeTime;

    [DataField]
    public float StunArea = 0.5f;
}
