using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Telephone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCTelephoneSystem))]
public sealed partial class RMCPickedUpPhoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Range = 7;

    [DataField, AutoNetworkedField]
    public bool CanTransfer = true;

    [DataField, AutoNetworkedField]
    public EntityUid? OriginalHolder;
}
