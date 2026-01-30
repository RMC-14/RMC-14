using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vendors;


[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCRequisitionsVendorComponent : Component
{
    /// <summary>
    /// Determines if the auto dispense to the table is enabled, this is just a toggle and still requires the Requisitions Chair Marker to exist.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled { get; set; } = true;
}
