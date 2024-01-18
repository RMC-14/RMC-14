using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Vendors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCMAutomatedVendorSystem))]
public sealed partial class CMVendorUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, int> Choices = new();

    [DataField, AutoNetworkedField]
    public int Points;
}
