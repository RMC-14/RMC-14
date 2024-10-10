using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Stun;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCStunOnHitComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityCoordinates? ShotFrom;

    [DataField, AutoNetworkedField]
    public float MaxRange = 2;

    [DataField, AutoNetworkedField]
    public bool LosesEffectWithRange = false;

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(1.4);

    [DataField, AutoNetworkedField]
    public TimeSpan SuperSlowTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public TimeSpan SlowTime = TimeSpan.FromSeconds(4);
}
