using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Buildup;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHealingReductionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Magnitude;

    [DataField, AutoNetworkedField]
    public float DissipationRate = 0.4f;

    [DataField, AutoNetworkedField]
    public float MaxBuildup = 50f;
}
