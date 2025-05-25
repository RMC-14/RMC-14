namespace Content.Server._RMC14.NPC.HTN;

[RegisterComponent]
[Access(typeof(RMCNPCSystem))]
public sealed partial class WakeNPCOnDropshipLandingComponent : Component
{
    [DataField]
    public bool FirstOnly = true;

    [DataField]
    public bool Attempted;

    [DataField]
    public int Range = 30;
}
