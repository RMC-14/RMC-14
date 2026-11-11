namespace Content.Shared._RMC14.Hallucination.Events;

[RegisterComponent]
public sealed partial class RMCHallucinationDoorPryComponent : Component
{
    [DataField]
    public float MinDistance = 2;
}
