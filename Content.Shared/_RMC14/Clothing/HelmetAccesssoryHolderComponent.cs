using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Clothing;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HelmetAccessoriesSystem))]
public sealed partial class HelmetAccessoryHolderComponent : Component {}

public enum HelmetAccessoryLayers
{
    Helmet
}
