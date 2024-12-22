using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class RMCRecentlyVendedComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> PreventCollide = new();
}
