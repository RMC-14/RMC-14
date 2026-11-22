using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Stun;

/// <summary>
///     Having this component prevents being dazed again.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCDazedSystem))]
public sealed partial class RMCDazedComponent : Component
{
    [DataField, AutoNetworkedField]
    public float VisionReduction = 0.5f;

    [DataField, AutoNetworkedField]
    public float OuterFadeStart;

    [DataField, AutoNetworkedField]
    public float OuterFadeEnd = 0.8f;

    [DataField, AutoNetworkedField]
    public float Alpha = 1;

    [DataField, AutoNetworkedField]
    public float InnerAlpha;

    [DataField, AutoNetworkedField]
    public Color Color = Color.Black;
}
