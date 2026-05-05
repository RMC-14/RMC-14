# Requisition Computer
requisition-paperwork-receiver-name = Logistics Branch
requisition-paperwork-reward-message = Confirmation Received! transferred ${$amount} from budget surplus
rmc-requisitions-window-title = Automated Storage and Retrieval System
rmc-requisitions-tab-products = Items
rmc-requisitions-tab-cart = Cart
rmc-requisitions-tab-pending = Pending orders
rmc-requisitions-platform-raise = Raise platform
rmc-requisitions-platform-lower = Lower platform
rmc-requisitions-platform-busy = Platform busy
rmc-requisitions-platform-missing = No platform
rmc-requisitions-search-placeholder = Search items...
rmc-requisitions-balance = [bold]Supply budget:[/bold] ${$balance}
rmc-requisitions-capacity = [bold]Elevator capacity:[/bold] {$count}/{$capacity}
rmc-requisitions-categories-title = Categories
rmc-requisitions-category-all = All
rmc-requisitions-category-ammo = Ammo
rmc-requisitions-category-attachments = Attachments
rmc-requisitions-category-clothing = Clothing
rmc-requisitions-category-engineering = Engineering
rmc-requisitions-category-explosives = Explosives
rmc-requisitions-category-food = Food
rmc-requisitions-category-gear = Gear
rmc-requisitions-category-medical = Medical
rmc-requisitions-category-mortar = Mortar
rmc-requisitions-category-operations = Operations
rmc-requisitions-category-reagent-tanks = Reagent tanks
rmc-requisitions-category-research = Research
rmc-requisitions-category-restricted-equipment = Restricted Equipment
rmc-requisitions-category-supplies = Supplies
rmc-requisitions-category-vehicle-ammo = Vehicle Ammo
rmc-requisitions-category-weapons = Weapons
rmc-requisitions-category-weapons-specialist-ammo = Weapons Specialist Ammo
rmc-requisitions-products-header = [bold]Items:[/bold] {$category}
rmc-requisitions-products-empty = No matching items.
rmc-requisitions-card-cost = Cost: ${$cost}
rmc-requisitions-card-no-description = No description available.
rmc-requisitions-add-tooltip = Add to cart
rmc-requisitions-remove-tooltip = Remove from cart
rmc-requisitions-cart-title = Cart
rmc-requisitions-cart-empty = Cart is empty.
rmc-requisitions-cart-category-empty = No items from this category in cart.
rmc-requisitions-cart-filter-empty = No matching cart items.
rmc-requisitions-cart-row-cost = Line total: ${$cost}
rmc-requisitions-cart-total = [bold]Total:[/bold] ${$total}
rmc-requisitions-cart-clear = Clear cart
rmc-requisitions-cart-insufficient-funds = Insufficient supply budget.
rmc-requisitions-cart-insufficient-capacity = Not enough elevator capacity.
rmc-requisitions-buy = Buy
rmc-requisitions-buy-confirm = Confirm purchase
rmc-requisitions-pending-empty = No pending orders.
rmc-requisitions-pending-category-empty = No pending orders from this category.
rmc-requisitions-pending-filter-empty = No matching pending orders.
rmc-requisitions-pending-quantity = Ordered: {$amount}

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
