namespace Content.Client._RMC14.Requisitions;

public sealed partial class RequisitionsBui
{
    protected override void Dispose(bool disposing)
    {
        if (!disposing)
        {
            base.Dispose(false);
            return;
        }
        if (_uiDisposed)
            return;

        _uiDisposed = true;
        IsOpened = false;
        _prototypes.PrototypesReloaded -= OnCatalogPrototypesReloaded;
        try
        {
            if (_window != null)
                ClearTextCache(_window);
            base.Dispose(true);
        }
        finally
        {
            ClearViewCache();
        }
    }

    private void ClearViewCache()
    {
        _supplyCatalog.Clear();
        _blackMarketCatalog.Clear();
        _productCards.Clear();
        _productCards.TrimExcess();
        _cart.Clear();
        _cart.TrimExcess();
        _blackMarketCart.Clear();
        _blackMarketCart.TrimExcess();
        _cartRows.Clear();
        _cartRows.TrimExcess();
        _shownCartRows.Clear();
        _shownCartRows.TrimExcess();
        _pendingCards.Clear();
        _pendingCards.TrimExcess();
        _requestCards.Clear();
        _requestCards.TrimExcess();
        _presentRequests.Clear();
        _presentRequests.TrimExcess();
        _obsoleteRequests.Clear();
        _obsoleteRequests.TrimExcess();
        _requestLines.Clear();
        _requestLines.TrimExcess();
        _requestRevisions.Clear();
        _requestRevisions.TrimExcess();
        _requestCosts.Clear();
        _requestCosts.TrimExcess();
        _renderedCatalog = null;
        _cartCatalog = null;
        _categoryView = null;
        _cartTotals = null;
        _blackMarketCategoryHeader = null;
        _mendozaDialogue = null;
        _selectedCategory = null;
        _selectedBlackMarketCategory = null;
        _state = null;
        _stockPlan = null;
        _window = null;
    }
}
