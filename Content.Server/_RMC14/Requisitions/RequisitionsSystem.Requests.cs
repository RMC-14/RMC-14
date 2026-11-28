using System.Linq;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Requisitions;

public sealed partial class RequisitionsSystem
{
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly SharedIdCardSystem _id = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private void InitializeRequests()
    {
        Subs.BuiEvents<RequisitionsComputerComponent>(RequisitionsUIKey.Key, subs =>
        {
            subs.Event<RequisitionsSubmitRequestMsg>(OnSubmitRequest);
            subs.Event<RequisitionsRespondRequestMsg>(OnRespondRequest);
        });
    }

    private bool CanManageRequisitions(Entity<RequisitionsComputerComponent> computer, EntityUid actor)
    {
        return !computer.Comp.RequestOnly && _access.IsAllowed(actor, computer);
    }

    private void OnSubmitRequest(Entity<RequisitionsComputerComponent> computer, ref RequisitionsSubmitRequestMsg args)
    {
        if (!computer.Comp.RequestOnly || !HasComp<HumanoidAppearanceComponent>(args.Actor) ||
            !_access.IsAllowed(args.Actor, computer))
        {
            RequestResult(computer, args.Actor, false, "rmc-requisitions-request-not-allowed");
            return;
        }

        if (args.Reason == null || args.Items == null ||
            args.Reason.Length > computer.Comp.MaxRequestReasonLength ||
            args.Items.Count > computer.Comp.MaxRequestLines)
        {
            RequestResult(computer, args.Actor, false, "rmc-requisitions-request-invalid-items");
            return;
        }

        if (computer.Comp.Account is not { } accountUid ||
            !TryComp(accountUid, out RequisitionsAccountComponent? account))
        {
            RequestResult(computer, args.Actor, false, "rmc-requisitions-request-unavailable");
            return;
        }

        var reason = args.Reason.Trim();
        if (reason.Length == 0 || reason.Length > computer.Comp.MaxRequestReasonLength)
        {
            RequestResult(computer, args.Actor, false, "rmc-requisitions-request-invalid-reason");
            return;
        }

        if (account.Requests.Count >= account.MaxRequests)
        {
            RequestResult(computer, args.Actor, false, "rmc-requisitions-request-queue-full");
            return;
        }

        if (!TryBuildRequest(computer, args.Items, out var items, out var totalCost))
        {
            RequestResult(computer, args.Actor, false, "rmc-requisitions-request-invalid-items");
            return;
        }

        GetRequestIdentity(args.Actor, out var requester, out var rank);
        var request = new RequisitionsRequest(account.NextRequestNumber++, requester, rank, reason, items, totalCost);
        account.Requests.Add(request);
        PrintRequest(computer, request);
        SendUIStateAll();
        RequestResult(computer, args.Actor, true, "rmc-requisitions-request-submitted", ("number", request.Number));
    }

    private void GetRequestIdentity(EntityUid actor, out string requester, out string rank)
    {
        requester = Identity.Name(actor, EntityManager);
        rank = Loc.GetString("rmc-requisitions-request-unknown-rank");
        if (_inventory.TryGetSlotEntity(actor, "id", out var worn) &&
            _id.TryGetIdCard(worn.Value, out var card))
        {
            if (!string.IsNullOrWhiteSpace(card.Comp.FullName))
                requester = card.Comp.FullName;
            var jobTitle = card.Comp._jobTitle;
            if (string.IsNullOrWhiteSpace(jobTitle) && card.Comp.JobTitle is { } title)
                jobTitle = Loc.GetString(title);
            if (!string.IsNullOrWhiteSpace(jobTitle))
                rank = jobTitle;
        }
    }

    private bool TryBuildRequest(
        Entity<RequisitionsComputerComponent> computer,
        List<RequisitionsCartItem> cart,
        out List<RequisitionsPendingOrder> items,
        out int totalCost)
    {
        items = new List<RequisitionsPendingOrder>();
        totalCost = 0;
        if (cart.Count == 0 || cart.Count > computer.Comp.MaxRequestLines)
            return false;

        var entries = 0;
        foreach (var category in computer.Comp.Categories)
        {
            entries += category.Entries.Count;
        }
        if (cart.Count > entries)
            return false;

        var quantities = new Dictionary<(int Category, int Entry), int>();
        foreach (var item in cart)
        {
            if (item == null || item.Amount <= 0 || item.Amount > computer.Comp.MaxRequestItems ||
                item.Category < 0 || item.Category >= computer.Comp.Categories.Count)
            {
                return false;
            }

            var category = computer.Comp.Categories[item.Category];
            if (item.Order < 0 || item.Order >= category.Entries.Count)
                return false;

            var entry = category.Entries[item.Order];
            if (entry.BlackMarket || entry.BlackMarketCost != 0 || entry.Cost < 0 ||
                !_prototype.HasIndex(entry.Crate) ||
                (long) entry.Cost * item.Amount + totalCost > int.MaxValue)
            {
                return false;
            }

            totalCost += entry.Cost * item.Amount;
            var key = (item.Category, item.Order);
            var quantity = quantities.GetValueOrDefault(key);
            if (item.Amount > computer.Comp.MaxRequestItems - quantity)
                return false;
            quantities[key] = quantity + item.Amount;
        }

        foreach (var (key, amount) in quantities)
        {
            var entry = computer.Comp.Categories[key.Category].Entries[key.Entry];
            items.Add(new RequisitionsPendingOrder(entry, amount));
        }

        return true;
    }

    private void OnRespondRequest(
        Entity<RequisitionsComputerComponent> computer,
        ref RequisitionsRespondRequestMsg args)
    {
        if (!CanManageRequisitions(computer, args.Actor) || !HasComp<HumanoidAppearanceComponent>(args.Actor) ||
            computer.Comp.Account is not { } accountUid ||
            !TryComp(accountUid, out RequisitionsAccountComponent? account))
            return;

        var number = args.Number;
        if (!computer.Comp.InsertedRequests.Contains(number))
            return;
        var request = account.Requests.Find(value => value.Number == number);
        if (request == null || request.Closed)
            return;
        if (request.Revision != args.Revision)
        {
            RequestPopup(computer, args.Actor, "rmc-requisitions-request-changed");
            return;
        }
        if (!args.Approve)
        {
            request.Closed = true;
            request.Revision++;
            UpdateRequestPapers(accountUid, request);
            EjectForm(computer, args.Actor, request.Number);
            if (request.Items.All(item => item.InTransit == 0))
                account.Requests.Remove(request);
            SendUIStateAll();
            return;
        }

        var selections = args.Selections ?? request.Items.Select((item, index) =>
            new RequisitionsRequestSelection(index, item.Remaining, true)).ToList();
        if (selections.Count > request.Items.Count)
            return;

        var stock = new RequisitionsStockPlan(ReadSupplyStock(computer, out var sources, args.Actor));
        var selected = new HashSet<int>();
        var issues = new List<(int Line, List<RequisitionsStockAllocation> Allocations)>();
        var purchases = new List<(RequisitionsEntry Product, int Amount, int Line)>();
        long cost = 0;
        var processing = 0;
        foreach (var selection in selections)
        {
            if (selection == null || selection.Line < 0 || selection.Line >= request.Items.Count ||
                !selected.Add(selection.Line))
                return;
            var item = request.Items[selection.Line];
            if (selection.Amount < 0 || selection.Amount > item.Remaining)
                return;
            processing += selection.Amount;
            if (processing > Math.Max(1, computer.Comp.MaxProcessingAmount))
            {
                RequestPopup(computer, args.Actor, "rmc-requisitions-process-limit",
                    ("limit", Math.Max(1, computer.Comp.MaxProcessingAmount)));
                return;
            }
            var allocations = new List<RequisitionsStockAllocation>();
            var issue = item.Entry.Unit ? stock.Reserve(item.Entry.Crate, selection.Amount, allocations) : 0;
            if (issue > 0)
            {
                issues.Add((selection.Line, allocations));
            }
            var purchase = selection.Purchase && (!item.Entry.Unit || item.Entry.Cost > 0)
                ? selection.Amount - issue : 0;
            if (purchase > 0)
            {
                purchases.Add((item.Entry, purchase, selection.Line));
                cost += (long) purchase * item.Entry.Cost;
            }
        }

        GetRequestIdentity(args.Actor, out var approver, out _);
        var elevator = GetElevator(computer);
        var orders = new List<RequisitionsEntry>();
        string? purchaseFailure = null;
        if (purchases.Count > 0)
        {
            if (cost > account.Balance || cost > int.MaxValue)
                purchaseFailure = "rmc-requisitions-cart-insufficient-funds";
            else
            {
                var details = new RequisitionsOrderDetails(request.Number, request.RequestedBy, approver,
                    GetNetEntity(accountUid));
                orders = PrepareShipments(computer, purchases, details);
                if (elevator == null ||
                    orders.Count > GetElevatorCapacity(elevator.Value) - elevator.Value.Comp.Orders.Count)
                    purchaseFailure = "rmc-requisitions-cart-insufficient-capacity";
            }
        }
        if (purchaseFailure != null)
        {
            purchases.Clear();
            orders.Clear();
            cost = 0;
        }

        if (args.ExpectedCost is { } expected && cost > expected)
        {
            RefreshUnitStock(computer);
            SendUIStateAll();
            RequestPopup(computer, args.Actor, "rmc-requisitions-request-changed");
            return;
        }
        var issued = 0;
        foreach (var issue in issues)
        {
            var item = request.Items[issue.Line];
            var amount = IssueUnits(args.Actor, issue.Allocations, sources);
            item.Fulfilled += amount;
            issued += amount;
        }
        if (orders.Count == 0 && issued == 0)
        {
            RequestPopup(computer, args.Actor, purchaseFailure ?? "rmc-requisitions-request-cannot-approve");
            return;
        }
        if (orders.Count > 0)
        {
            account.Balance -= (int) cost;
            foreach (var purchase in purchases)
                request.Items[purchase.Line].InTransit += purchase.Amount;
            elevator!.Value.Comp.Orders.AddRange(orders);
            Dirty(elevator.Value);
            Dirty(accountUid, account);
        }
        request.Revision++;
        UpdateRequestPapers(accountUid, request);
        RefreshUnitStock(computer);
        SendUIStateAll();
        if (purchaseFailure != null)
            RequestPopup(computer, args.Actor, "rmc-requisitions-purchase-deferred");
        _adminLogs.Add(LogType.RMCRequisitionsBuy,
            $"{ToPrettyString(args.Actor):actor} processed request #{request.Number} by {request.RequestedBy}: " +
            $"issued {issued}, purchased {orders.Count} shipments for {cost}");
        _core.CreateARESLog(computer.Owner, LogCat,
            $"{approver} processed requisition #{request.Number}: " +
            $"issued {issued}, {orders.Count} shipments for {cost}$");
    }

    private void RequestPopup(EntityUid computer, EntityUid actor, string message, params (string, object)[] args)
    {
        _popup.PopupEntity(Loc.GetString(message, args), computer, actor);
    }

    private void RequestResult(
        EntityUid computer,
        EntityUid actor,
        bool success,
        string message,
        params (string, object)[] args)
    {
        _ui.ServerSendUiMessage(computer, RequisitionsUIKey.Key,
            new RequisitionsRequestResultMsg(success, Loc.GetString(message, args)), actor);
    }
}
