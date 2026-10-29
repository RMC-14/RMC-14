namespace Content.Shared._RMC14.Chemistry.Disabilities;

// TODO RMC14: cm13 cures this with brain surgery
[RegisterComponent, Access(typeof(RMCNervousSystem))]
public sealed partial class RMCNervousComponent : Component
{
    [DataField]
    public TimeSpan NextCheck;
}
