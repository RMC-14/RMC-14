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
rmc-requisitions-card-cost-wy = Cost: WY${$cost}
rmc-requisitions-card-cost-dual = Cost: ${$cost} / WY${$wy}
rmc-requisitions-card-no-description = No description available.
rmc-requisitions-add-tooltip = Add to cart
rmc-requisitions-remove-tooltip = Remove from cart
rmc-requisitions-cart-title = Cart
rmc-requisitions-cart-empty = Cart is empty.
rmc-requisitions-cart-category-empty = No items from this category in cart.
rmc-requisitions-cart-filter-empty = No matching cart items.
rmc-requisitions-cart-row-cost = Line total: ${$cost}
rmc-requisitions-cart-row-cost-wy = Line total: WY${$cost}
rmc-requisitions-cart-row-cost-dual = Line total: ${$cost} / WY${$wy}
rmc-requisitions-cart-total = [bold]Total:[/bold] ${$total}
rmc-requisitions-cart-total-wy = [bold]Total:[/bold] WY${$total}
rmc-requisitions-cart-total-dual = [bold]Total:[/bold] ${$total} / WY${$wy}
rmc-requisitions-cart-clear = Clear cart
rmc-requisitions-cart-insufficient-funds = Insufficient supply budget.
rmc-requisitions-cart-insufficient-wy = Insufficient WY funds.
rmc-requisitions-cart-insufficient-capacity = Not enough elevator capacity.
rmc-requisitions-buy = Buy
rmc-requisitions-buy-confirm = Confirm purchase
rmc-requisitions-pending-empty = No pending orders.
rmc-requisitions-pending-category-empty = No pending orders from this category.
rmc-requisitions-pending-filter-empty = No matching pending orders.
rmc-requisitions-pending-quantity = Ordered: {$amount}
rmc-requisitions-black-market-button = $E4RR301¿
rmc-requisitions-black-market-return = ASRS CATALOG
rmc-requisitions-black-market-balance = [bold]Supply budget:[/bold] ${$balance} | [bold]WY account:[/bold] WY${$wy} | [bold]Heat:[/bold] {$heat}/100
rmc-requisitions-black-market-category-seized-items = Seized Items
rmc-requisitions-black-market-category-shipside-contraband = Shipside Contraband
rmc-requisitions-black-market-category-surplus-equipment = Surplus Equipment
rmc-requisitions-black-market-category-contraband-ammo = Contraband Ammo
rmc-requisitions-black-market-category-deep-storage = Deep Storage
rmc-requisitions-black-market-category-miscellaneous = Miscellaneous
rmc-requisitions-black-market-scanner-crate-name = black market scanner crate
rmc-requisitions-black-market-scanner-crate-desc = An interesting wooden crate.
rmc-requisitions-black-market-unavailable = Black market unavailable.
rmc-requisitions-black-market-locked-out = Tradeband locked by external compliance action.
rmc-requisitions-black-market-mendoza-dead = Mendoza is not responding.
rmc-requisitions-black-market-hack-already = The console is already tuned to an irregular tradeband.
rmc-requisitions-black-market-hack-unavailable = The irregular tradeband refuses the handshake.
rmc-requisitions-black-market-hack-no-skill = You have no idea what you're doing.
rmc-requisitions-black-market-hack-start = You start messing around with the electronics of {$target}...
rmc-requisitions-black-market-hack-bus = Huh? You find a processor bus with the letters 'B.M.' written in white crayon over it. You start fiddling with it.
rmc-requisitions-black-market-hack-enable = You amplify the broadcasting function with {$tool}, and a red light starts blinking on and off on the board. Put it back in?
rmc-requisitions-black-market-hack-disable = You weaken the broadcasting function with {$tool}, and the red light stops blinking, turning off. It's probably good now.
rmc-requisitions-black-market-scan-value = ITEM HAS WY${$value} VALUE
rmc-requisitions-black-market-scan-no-value = ITEM HAS NO BLACK MARKET VALUE
rmc-requisitions-black-market-scan-danger = LIVE SHIPMENT WOULD TERMINATE THE TRADEBAND
rmc-requisitions-black-market-sell-message = Confirmation Received! transferred WY${$amount} through irregular tradeband
rmc-requisitions-black-market-mendoza-dead-message = Irregular tradeband lost. Mendoza is not responding.

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
