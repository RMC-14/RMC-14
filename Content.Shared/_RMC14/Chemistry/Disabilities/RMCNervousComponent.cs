namespace Content.Shared._RMC14.Chemistry.Disabilities;

[RegisterComponent, Access(typeof(RMCNervousSystem))]
public sealed partial class RMCNervousComponent : Component
{
    [DataField]
    public TimeSpan NextCheck;
}
