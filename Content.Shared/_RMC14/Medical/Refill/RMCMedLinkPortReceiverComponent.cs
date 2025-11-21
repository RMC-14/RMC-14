using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMedicalSupplyLinkSystem))]
public sealed partial class RMCMedLinkPortReceiverComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool AllowSupplyLinkRestock = true;

    [DataField, AutoNetworkedField]
    public int RestockMinimumRoundTime = 20; // 20 Minutes

    [DataField, AutoNetworkedField]
    public float RestockIntervalSeconds = 30f; // PROCESSING_SUBSYSTEM_DEF(slowobj)

    [DataField, AutoNetworkedField]
    public float RestockChancePerItem = 0.2f; // 20% chance to restock each item per check
}
