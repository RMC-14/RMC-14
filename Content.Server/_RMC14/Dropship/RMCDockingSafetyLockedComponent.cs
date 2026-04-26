namespace Content.Server._RMC14.Dropship;

[RegisterComponent]
public sealed partial class RMCDockingSafetyLockedComponent : Component
{
    [ViewVariables]
    public bool WasBoltedBeforeSafetyLock;

    [ViewVariables]
    public bool Active;
}
