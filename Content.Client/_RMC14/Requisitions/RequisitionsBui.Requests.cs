using Content.Shared._RMC14.Requisitions;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Requisitions;

public sealed partial class RequisitionsBui
{
    private readonly Dictionary<int, RequisitionsRequestCard> _requestCards = new();
    private readonly HashSet<int> _presentRequests = new();
    private readonly List<int> _obsoleteRequests = new();

    private bool _submittingRequest;
    private bool RequestOnly => _state is { RequestOnly: true };

    private void SetCartAmount(CartKey key, int amount)
    {
        if (_state == null || _submittingRequest || !TryGetEntry(key, out var entry))
            return;

        var current = GetCartAmount(key);
        var maximum = Math.Max(0, _state.MaxRequestItems);
        if (!RequestOnly && entry.Unit && entry.Cost <= 0)
            maximum = 0;
        if (RequestOnly)
        {
            if (entry.Cost > 0)
            {
                var otherCost = GetCartSupplyTotal() - (long) current * entry.Cost;
                maximum = (int) Math.Clamp((int.MaxValue - otherCost) / entry.Cost, 0, maximum);
            }
        }
        else
        {
            if (entry.Cost > 0)
                maximum = (int) Math.Clamp(
                    (_state.Balance - GetCartSupplyTotal() + (long) current * entry.Cost) / entry.Cost, 0, maximum);
            if (entry.BlackMarketCost > 0)
            {
                var available = _state.BlackMarketBalance - GetCartBlackMarketTotal() +
                                (long) current * entry.BlackMarketCost;
                maximum = (int) Math.Clamp(available / entry.BlackMarketCost, 0, maximum);
            }
        }

        if (!RequestOnly)
        {
            while (maximum > 0 && GetCartSlots(key, maximum) > GetRemainingCapacity())
                maximum--;
        }
        amount = Math.Clamp(amount, 0, maximum);
        if (amount == current)
            return;

        if (amount == 0)
            GetCurrentCart().Remove(key);
        else
            GetCurrentCart()[key] = amount;
        if (_window != null)
            _window.BuyButton.IsConfirming = false;
        RefreshCart();
        UpdateVisibleProductCards();
    }

    private void UpdateRequestMode()
    {
        if (_window == null)
            return;

        var title = Loc.GetString(RequestOnly
            ? "rmc-requisitions-request-window-title"
            : "rmc-requisitions-window-title");
        if (_window.Title != title)
            _window.Title = title;
        _window.PlatformButton.Visible = !RequestOnly;
        _window.CapacityLabel.Visible = !RequestOnly;
        _window.RequestReasonPanel.Visible = RequestOnly;
        TabContainer.SetTabVisible(_window.PendingTab, !RequestOnly);
        TabContainer.SetTabVisible(_window.RequestsTab, !RequestOnly);
        TabContainer.SetTabTitle(_window.RequestsTab,
            Loc.GetString("rmc-requisitions-tab-requests-count", ("count", _state?.Requests.Count ?? 0)));
        var buyText = Loc.GetString(RequestOnly
            ? "rmc-requisitions-request-submit"
            : "rmc-requisitions-buy");
        if (_window.BuyButton.Text != buyText)
            _window.BuyButton.Text = buyText;
        _window.BuyButton.ConfirmationText = Loc.GetString(RequestOnly
            ? "rmc-requisitions-request-submit-confirm"
            : "rmc-requisitions-buy-confirm");
        if (RequestOnly)
            _blackMarketMode = false;
    }

    private void SubmitRequest(List<RequisitionsCartItem> items)
    {
        if (_window == null || _submittingRequest || string.IsNullOrWhiteSpace(_window.RequestReason.Text))
            return;

        _submittingRequest = true;
        SendMessage(new RequisitionsSubmitRequestMsg(items, _window.RequestReason.Text.Trim()));
        RefreshCart();
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is not RequisitionsRequestResultMsg result || _window == null)
            return;

        _submittingRequest = false;
        _window.RequestFeedback.SetMessage(FormattedMessage.FromUnformatted(result.Message));
        if (result.Success)
        {
            _cart.Clear();
            _window.RequestReason.Text = string.Empty;
        }

        RefreshVisibleEntries();
    }

    private void PopulateRequests()
    {
        if (_window == null || _state == null || RequestOnly || !_window.RequestsTab.Visible)
            return;

        _presentRequests.Clear();
        var search = _window.SearchBar.Text.Trim();
        var visible = 0;
        foreach (var request in _state.Requests)
        {
            _presentRequests.Add(request.Number);
            if (!RequestMatchesSearch(request, search))
            {
                if (_requestCards.TryGetValue(request.Number, out var hidden))
                    hidden.Visible = false;
                continue;
            }

            if (!_requestCards.TryGetValue(request.Number, out var card))
            {
                card = new RequisitionsRequestCard();
                var number = request.Number;
                card.Approve.OnPressed += _ => RespondToRequest(number, true);
                card.Deny.OnPressed += _ => RespondToRequest(number, false);
                card.Eject.OnPressed += _ => SendMessage(new RequisitionsEjectRequestMsg(number));
                _requestCards[number] = card;
                _window.RequestsContainer.AddChild(card);
            }

            card.Visible = true;
            SetText(card.Title, Loc.GetString("rmc-requisitions-request-title", ("number", request.Number)));
            SetText(card.Requester, Loc.GetString(
                "rmc-requisitions-request-requester",
                ("name", request.RequestedBy), ("rank", request.Rank)));
            SetText(card.Reason, Loc.GetString("rmc-requisitions-request-reason-text", ("reason", request.Reason)));
            PopulateRequestLines(request, card);
            card.Eject.Visible = System.Linq.Enumerable.Contains(_state.InsertedRequests, request.Number);
            SetText(card.Cost, Loc.GetString("rmc-requisitions-request-value", ("total", request.TotalCost)));
            visible++;
        }

        _obsoleteRequests.Clear();
        foreach (var number in _requestCards.Keys)
        {
            if (!_presentRequests.Contains(number))
                _obsoleteRequests.Add(number);
        }

        foreach (var number in _obsoleteRequests)
        {
            ForgetRequestLines(number);
            _requestCards[number].Dispose();
            _requestCards.Remove(number);
        }

        SetText(_window.RequestsStatus, visible > 0
            ? string.Empty
            : Loc.GetString("rmc-requisitions-request-list-empty"));
    }

    private bool RequestMatchesSearch(RequisitionsRequest request, string search)
    {
        if (string.IsNullOrEmpty(search) ||
            request.Number.ToString().Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
            request.RequestedBy.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
            request.Reason.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            return true;

        foreach (var item in request.Items)
        {
            var text = Loc.GetString("rmc-requisitions-request-item", ("amount", item.Amount),
                ("name", GetDisplayText(item.Entry).Name), ("cost", item.Entry.Cost * item.Amount));
            if (text.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                return true;
        }
        return false;
    }
}
