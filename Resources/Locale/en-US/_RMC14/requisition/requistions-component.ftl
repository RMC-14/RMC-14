# Requisition Computer
requisition-paperwork-receiver-name = Logistics Branch
requisition-paperwork-reward-message = Confirmation Received! transferred ${$amount} from budget surplus

rmc-req-no-platform = No platform
rmc-req-platform-pos = Platform position: {$position}
rmc-req-asrs-is-busy = ASRS is busy
rmc-req-raise-platform = Raise platform
rmc-req-platform-pos-low = Platform position: Lowered
rmc-req-lower-platform = Lower platform
rmc-req-platform-pos-raise = Platform position: Raised
rmc-req-asrs-please-wait = Please wait
rmc-req-lower-platform-wait = Platform lowering...
rmc-req-raise-platform-wait = Platform raising...
rmc-req-supply-budget = [bold]Supply budget: {$balance}[/bold]
rmc-req-select-category = [bold]Select a category[/bold]
rmc-req-request-from-category = [bold]Request from: {$category}[/bold]
rmc-req-back-to-all-cat = Back to all categories
rmc-req-order-items = Order items
rmc-req-view-requests = View requests
rmc-req-view-orders = View orders
rmc-req-main-menu = Main menu
rmc-req-search-item = Search item

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
