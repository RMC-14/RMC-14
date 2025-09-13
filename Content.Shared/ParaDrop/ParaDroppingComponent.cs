using Robust.Shared.GameStates;

namespace Content.Shared.ParaDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParaDroppingComponent : Component
{
    [DataField, AutoNetworkedField]
    public float RemainingTime;

    [DataField, AutoNetworkedField]
    public Dictionary<string, int> OriginalLayers = new();

    [DataField, AutoNetworkedField]
    public Dictionary<string, int> OriginalMasks = new();
}
