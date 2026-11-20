using System.Linq;
using Content.Shared._RMC14.Requisitions;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Requisitions;

public sealed partial class RequisitionsBui
{
    private readonly Dictionary<(int Request, int Line), RequisitionsRequestLine> _requestLines = new();
    private readonly Dictionary<int, int> _requestRevisions = new();
    private readonly Dictionary<int, int> _requestCosts = new();
    private bool _updatingRequestLines;
    private bool _initialRequestCategory;

    private void SelectInitialRequestCategory()
    {
        if (_initialRequestCategory || !RequestOnly || _state == null)
            return;
        for (var i = 0; i < _state.Categories.Count; i++)
        {
            if (!_state.Categories[i].Entries.Any(entry => entry.Unit))
                continue;
            SelectedCategory = i;
            _initialRequestCategory = true;
            return;
        }
    }

    private string GetShipmentContents(RequisitionsEntry entry)
    {
        return string.Join(", ", entry.Entities.GroupBy(id => id).Select(group =>
            Loc.GetString("rmc-requisitions-contents-item", ("amount", group.Count()),
                ("name", _prototypes.TryIndex(group.Key, out var prototype) ? prototype.Name : group.Key.Id))));
    }

    private static string StockText(int amount)
    {
        return amount == int.MaxValue ? "∞" : amount.ToString();
    }

    private int GetCartSlots(CartKey? changed = null, int value = 0)
    {
        if (_blackMarketMode)
            return GetCartAmount() - (changed is { } key ? GetCartAmount(key) - value : 0);

        var units = 0;
        var crates = 0;
        var keys = GetCurrentCart().Keys.ToHashSet();
        if (changed is { } added)
            keys.Add(added);
        foreach (var key in keys)
        {
            if (!TryGetEntry(key, out var product))
                continue;
            var amount = changed == key ? value : GetCartAmount(key);
            if (product.Unit)
                units += amount;
            else
                crates += amount;
        }
        var capacity = Math.Clamp(_state?.ShipmentItems ?? 20, 1, 20);
        return crates + (units + capacity - 1) / capacity;
    }

    private void RespondToRequest(int number, bool approve)
    {
        if (_state?.Requests.FirstOrDefault(request => request.Number == number) is not { } request)
            return;

        var message = new RequisitionsRespondRequestMsg(number, approve)
        {
            Revision = _requestRevisions.GetValueOrDefault(number),
            ExpectedCost = _requestCosts.GetValueOrDefault(number),
            Selections = new(),
        };
        for (var i = 0; i < request.Items.Count; i++)
        {
            if (_requestLines.TryGetValue((number, i), out var row))
                message.Selections.Add(new RequisitionsRequestSelection(i, row.Quantity.Value, row.Purchase.Pressed));
        }
        SendMessage(message);
    }

    private void PopulateRequestLines(RequisitionsRequest request, RequisitionsRequestCard card)
    {
        var changed = !_requestRevisions.TryGetValue(request.Number, out var revision) || revision != request.Revision;
        if (changed)
        {
            card.Approve.IsConfirming = false;
            card.Deny.IsConfirming = false;
        }
        _updatingRequestLines = true;
        for (var i = 0; i < request.Items.Count; i++)
        {
            var index = i;
            var item = request.Items[i];
            var created = false;
            if (!_requestLines.TryGetValue((request.Number, i), out var row))
            {
                created = true;
                row = new RequisitionsRequestLine();
                _requestLines[(request.Number, i)] = row;
                card.Lines.AddChild(row);
                var line = row;
                row.Quantity.IsValid = amount => amount >= 0;
                row.Quantity.ValueChanged += args =>
                {
                    if (_updatingRequestLines)
                        return;
                    var remaining = _state?.Requests.FirstOrDefault(value => value.Number == request.Number)
                        ?.Items[index].Remaining ?? 0;
                    line.Quantity.OverrideValue(Math.Clamp(args.Value, 0, remaining));
                    card.Approve.IsConfirming = false;
                    UpdateRequestPlan(request.Number);
                };
                row.Remove.OnPressed += _ => line.Quantity.Value = Math.Max(0, line.Quantity.Value - 1);
                row.Add.OnPressed += _ => line.Quantity.Value++;
                row.Purchase.OnToggled += _ =>
                {
                    if (!_updatingRequestLines)
                        card.Approve.IsConfirming = false;
                    UpdateRequestPlan(request.Number);
                };
            }
            SetText(row.Title, $"[bold]{FormattedMessage.EscapeText(GetDisplayText(item.Entry).Name)}[/bold]",
                markup: true, color: Color.White);
            SetText(row.Progress, Loc.GetString("rmc-requisitions-line-progress",
                ("requested", item.Amount), ("fulfilled", item.Fulfilled), ("transit", item.InTransit),
                ("remaining", item.Remaining)));
            row.Purchase.Disabled = item.Entry.Unit && item.Entry.Cost <= 0;
            if (changed && row.Quantity.Value != item.Remaining)
                row.Quantity.OverrideValue(item.Remaining);
            if (created || row.Purchase.Disabled)
                row.Purchase.Pressed = !row.Purchase.Disabled;
            row.Remove.Disabled = item.Remaining == 0;
            row.Add.Disabled = item.Remaining == 0;
            row.Quantity.LineEditControl.Editable = item.Remaining > 0;
        }
        _requestRevisions[request.Number] = request.Revision;
        _updatingRequestLines = false;
        UpdateRequestPlan(request.Number);
    }

    private void UpdateRequestPlan(int number)
    {
        if (_updatingRequestLines || _state == null || !_requestCards.TryGetValue(number, out var card) ||
            _state.Requests.FirstOrDefault(request => request.Number == number) is not { } request)
            return;

        var stock = new RequisitionsStockPlan(_state.Stock);
        var previews = new List<(RequisitionsRequestLine Row, int Stock, int Issue,
            int Missing, int Purchase, long Cost)>();
        long cost = 0;
        var units = 0;
        var crates = 0;
        var issued = 0;
        var processing = 0;
        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            var row = _requestLines[(number, i)];
            var amount = Math.Clamp(row.Quantity.Value, 0, item.Remaining);
            processing += amount;
            var available = stock.GetAvailable(item.Entry.Crate);
            var issue = item.Entry.Unit ? stock.Reserve(item.Entry.Crate, amount) : 0;
            var purchase = row.Purchase.Pressed && (!item.Entry.Unit || item.Entry.Cost > 0) ? amount - issue : 0;
            issued += issue;
            cost += (long) purchase * item.Entry.Cost;
            if (item.Entry.Unit)
                units += purchase;
            else
                crates += purchase;
            previews.Add((row, item.Entry.Unit ? available : 0, issue, amount - issue,
                purchase, (long) purchase * item.Entry.Cost));
        }
        var capacity = Math.Clamp(_state.ShipmentItems, 1, 20);
        var slots = crates + (units + capacity - 1) / capacity;
        var purchaseBlocked = slots > 0 && (cost > _state.Balance || slots > GetRemainingCapacity());
        foreach (var preview in previews)
        {
            SetText(preview.Row.Preview, Loc.GetString("rmc-requisitions-line-plan",
                ("stock", StockText(preview.Stock)), ("issue", preview.Issue)));
            var purchase = purchaseBlocked ? 0 : preview.Purchase;
            var purchaseText = purchase > 0
                ? Loc.GetString("rmc-requisitions-line-purchase", ("amount", purchase), ("cost", preview.Cost))
                : Loc.GetString(preview.Missing > 0
                    ? "rmc-requisitions-line-unordered" : "rmc-requisitions-line-no-purchase",
                    ("amount", preview.Missing));
            if (preview.Row.Purchase.Disabled)
                purchaseText = Loc.GetString("rmc-requisitions-stock-only");
            SetText(preview.Row.PurchasePreview, purchaseText);
            preview.Row.PurchasePreview.Visible = preview.Missing > 0;
        }
        var price = purchaseBlocked ? 0 : (int) Math.Min(int.MaxValue, cost);
        if (_requestCosts.TryGetValue(number, out var previous) && previous != price)
            card.Approve.IsConfirming = false;
        _requestCosts[number] = price;
        var plan = Loc.GetString("rmc-requisitions-processing-plan", ("issue", issued),
            ("purchase", purchaseBlocked ? 0 : units + crates),
            ("cost", price), ("slots", purchaseBlocked ? 0 : slots));
        var overLimit = processing > Math.Max(1, _state.MaxProcessingAmount);
        if (overLimit)
            plan += "\n" + Loc.GetString("rmc-requisitions-process-limit",
                ("limit", Math.Max(1, _state.MaxProcessingAmount)));
        else if (purchaseBlocked && cost > _state.Balance)
            plan += "\n" + Loc.GetString("rmc-requisitions-cart-insufficient-funds");
        else if (purchaseBlocked)
            plan += "\n" + Loc.GetString("rmc-requisitions-cart-insufficient-capacity");
        else if (issued == 0 && slots == 0)
            plan += "\n" + Loc.GetString("rmc-requisitions-no-processing");
        if (!overLimit && purchaseBlocked && issued > 0)
            plan += "\n" + Loc.GetString("rmc-requisitions-purchase-deferred");
        SetText(card.Plan, plan);
        card.Approve.Disabled = overLimit || (issued == 0 && (slots == 0 || purchaseBlocked));
    }

    private void ForgetRequestLines(int number)
    {
        foreach (var key in _requestLines.Keys.Where(key => key.Request == number).ToArray())
            _requestLines.Remove(key);
        _requestRevisions.Remove(number);
        _requestCosts.Remove(number);
    }
}
