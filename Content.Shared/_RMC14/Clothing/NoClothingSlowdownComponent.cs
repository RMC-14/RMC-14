using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCClothingSystem))]
public sealed partial class NoClothingSlowdownComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Slot = "shoes";

    [DataField, AutoNetworkedField]
    public float WalkModifier = 0.68f;

    [DataField, AutoNetworkedField]
    public float SprintModifier = 0.68f;

    [DataField, AutoNetworkedField]
    public bool Active = false;

}
