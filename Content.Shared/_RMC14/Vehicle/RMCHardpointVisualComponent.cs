namespace Content.Shared._RMC14.Vehicle;

[RegisterComponent]
public sealed partial class RMCHardpointVisualComponent : Component
{
    [DataField(required: true)]
    public string VehicleState = string.Empty;
}
