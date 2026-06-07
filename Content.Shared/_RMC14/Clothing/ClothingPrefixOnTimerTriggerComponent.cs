using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCClothingSystem))]
public sealed partial class ClothingPrefixOnTimerTriggerComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Prefix = "primed";

    [DataField, AutoNetworkedField]
    public string? OriginalPrefix;
}
