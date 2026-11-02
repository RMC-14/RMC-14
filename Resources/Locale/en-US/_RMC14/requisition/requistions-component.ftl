# Requisition Computer
requisition-paperwork-receiver-name = Logistics Branch
requisition-paperwork-reward-message = Confirmation Received! transferred ${$amount} from budget surplus

# Requisition Invoice
requisition-paper-print-name = {$name} invoice
requisition-paper-print-manifest = [head=2]
    {$containerName}[/head][bold]{$content}[/bold][head=2]
    WT. {$weight} LBS
    LOT {$lot}
    S/N {$serialNumber}[/head]
requisition-paper-print-content = - {$count} {$item}

# Supply Drop Console
ui-supply-drop-consle-name = Supply Drop Console
ui-supply-drop-console-name-bolded = [bold]SUPPLY DROP[/bold] 
ui-supply-drop-console-longitude = Longitude:
ui-supply-drop-console-latitude = Latitude:
ui-supply-drop-pad-status = [bold]Supply Pad Status[/bold]
ui-supply-drop-console-update = Update
ui-supply-drop-console-ready = Ready to fire!
ui-supply-drop-console-launch = LAUNCH SUPPLY DROP
ui-supply-drop-console-launch-confirmation = Confirm Supply Drop?
ui-supply-drop-console-cooldown = {$time} seconds until next launch
ui-supply-drop-crate-status =
    { $hasCrate ->
        [true] Supply Pad Status: crate loaded.
       *[false] No crate loaded.
    }

### Requisitions console
rmc-requisitions-ui-title = Automated Storage and Retrieval System
rmc-requisitions-ui-no-platform = No platform
rmc-requisitions-ui-busy = ASRS is busy
rmc-requisitions-ui-raise-platform = Raise platform
rmc-requisitions-ui-lower-platform = Lower platform
rmc-requisitions-ui-please-wait = Please wait
rmc-requisitions-ui-platform-lowered = Platform position: Lowered
rmc-requisitions-ui-platform-raised = Platform position: Raised
rmc-requisitions-ui-platform-lowering = Platform lowering...
rmc-requisitions-ui-platform-raising = Platform raising...
rmc-requisitions-ui-budget = [bold]Supply budget: ${$balance}[/bold]
rmc-requisitions-ui-select-category = [bold]Select a category[/bold]
rmc-requisitions-ui-request-from = [bold]Request from: {$category}[/bold]
rmc-requisitions-ui-back-to-categories = Back to all categories
rmc-requisitions-ui-order-items = Order items
rmc-requisitions-ui-view-requests = View requests
rmc-requisitions-ui-view-orders = View orders
rmc-requisitions-ui-main-menu = Main Menu
rmc-requisitions-ui-search-item = Search item
rmc-requisitions-ui-search-placeholder = Search...
