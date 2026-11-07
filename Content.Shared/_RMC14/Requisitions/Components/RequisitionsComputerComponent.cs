using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedRequisitionsSystem))]
public sealed partial class RequisitionsComputerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool RequestOnly;

    [DataField, AutoNetworkedField]
    public int MaxRequestItems = 99;

    [DataField, AutoNetworkedField]
    public int MaxRequestLines = 50;

    [DataField, AutoNetworkedField]
    public int MaxRequestReasonLength = 512;

    [DataField]
    public EntProtoId RequestPaper = "RMCPaperRequisitionForm";

    [DataField]
    public int RequestPaperMaxItems = 8;

    [DataField]
    public int RequestPaperContentsLength = 800;

    [DataField]
    public int RequestPaperReasonLength = 160;

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/print.ogg");

    [AutoNetworkedField]
    public List<RequisitionsRequest> Requests = new();

    [AutoNetworkedField]
    public RequisitionsStock Stock = new();

    [AutoNetworkedField]
    public List<int> InsertedRequests = new();

    public bool UnitsBuilt;

    [DataField]
    public EntProtoId UnitShipment = "RMCCrateRequisitionShipment";

    [DataField, AutoNetworkedField]
    public int ShipmentItems = 20;

    [DataField, AutoNetworkedField]
    public int MaxProcessingAmount = 500;

    [DataField]
    public Dictionary<EntProtoId, int> UnitPrices = new();

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
