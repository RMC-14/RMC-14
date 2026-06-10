using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRequisitionsSystem))]
public sealed partial class RequisitionsComputerComponent : Component
{
    [DataField]
    public EntityUid? Account;

    [DataField("soundIncomingSurplus")]
    public SoundSpecifier IncomingSurplus = new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg");

    [DataField]
    public EntityUid? Platform;

    [DataField(required: true), AutoNetworkedField, AlwaysPushInheritance]
    public List<RequisitionsCategory> Categories = new();

    [DataField, AutoNetworkedField, AlwaysPushInheritance]
    public List<RequisitionsCategory> BlackMarketCategories = new();

    [DataField, AutoNetworkedField]
    public bool BlackMarketUnlocked;

    [AutoNetworkedField]
    public RequisitionsElevatorMode? PlatformLowered;

    [AutoNetworkedField]
    public bool Busy;

    [AutoNetworkedField]
    public int Balance;

    [AutoNetworkedField]
    public bool Full;

    [AutoNetworkedField]
    public int OrderCount;

    [AutoNetworkedField]
    public int Capacity;

    [AutoNetworkedField]
    public int BlackMarketBalance;

    [AutoNetworkedField]
    public RequisitionsBlackMarketStatus BlackMarketStatus;

    [AutoNetworkedField]
    public List<RequisitionsPendingOrder> PendingOrders = new();

    [DataField]
    public bool IsLastInteracted = false;
}
