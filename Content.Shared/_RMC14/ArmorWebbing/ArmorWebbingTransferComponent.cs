using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ArmorWebbing;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedArmorWebbingSystem))]
public sealed partial class ArmorWebbingTransferComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Clothing;

    [DataField, AutoNetworkedField]
    public TransferType Transfer;

    [DataField, AutoNetworkedField]
    public bool Defer = true;

    public enum TransferType
    {
        ToClothing,
        ToArmorWebbing,
    }
}
