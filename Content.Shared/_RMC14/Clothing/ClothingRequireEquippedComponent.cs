using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCClothingSystem))]
public sealed partial class ClothingRequireEquippedComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntityWhitelist Whitelist = new();
}
