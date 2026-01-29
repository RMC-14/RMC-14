using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCClothingSystem))]
public sealed partial class ClothingFactionLockIgnoreComponent : Component
{
}
