using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Dropship;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCDockingSafetyLockedComponent : Component
{
    [DataField]
    public bool WasBoltedBeforeSafetyLock;

    [DataField]
    public bool Active;
}
