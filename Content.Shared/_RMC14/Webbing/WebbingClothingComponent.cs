using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Webbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedWebbingSystem))]
public sealed partial class WebbingClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Container = "cm_clothing_webbing_slot";

    [DataField, AutoNetworkedField]
    public EntityUid? Webbing;
}
