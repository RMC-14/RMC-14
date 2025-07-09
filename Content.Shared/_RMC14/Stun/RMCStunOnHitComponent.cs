using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Stun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCStunOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public MapCoordinates? ShotFrom;

    [DataField, AutoNetworkedField]
    public float MaxRange = 2.5f;

    [DataField, AutoNetworkedField]
    public float KnockBackPowerMin = 1;

    [DataField, AutoNetworkedField]
    public float KnockBackPowerMax = 1;

    [DataField, AutoNetworkedField]
    public float KnockBackSpeed = 5;

    [DataField, AutoNetworkedField]
    public bool ForceKnockBack;

    [DataField, AutoNetworkedField]
    public bool LosesEffectWithRange = false;

    [DataField, AutoNetworkedField]
    public bool SlowsEffectBigXenos = false;

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1.4);

    [DataField, AutoNetworkedField]
    public TimeSpan SuperSlowTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public TimeSpan DazeTime;

    [DataField, AutoNetworkedField]
    public float StunArea = 0.5f;
}
