using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Buildup;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHivemindInterferenceComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Magnitude;

    [DataField, AutoNetworkedField]
    public float DissipationRate = 1f;

    [DataField, AutoNetworkedField]
    public float MaxBuildup = 90f;
}
