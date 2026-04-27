rmc-ert-admin-command-player-only = This command can only be used by a player.
rmc-ert-admin-command-admin-only = This command can only be used by an active admin.
rmc-ert-admin-command-force-usage = Usage: rmcertcall <callId> [reason]
rmc-ert-admin-command-force-success = Forced ERT request {$id} for {$call}.
rmc-ert-admin-command-force-call-hint = ERT call prototype
rmc-ert-admin-command-force-reason-hint = Optional reason
rmc-ert-admin-command-force-list-empty = No ERT calls are available for force calling.
rmc-ert-admin-command-force-list-entry = {$id} | {$name} | {$category} | {$organization}

rmc-ert-admin-window-title = ERT Dispatch
rmc-ert-admin-window-header = Emergency Response Team requests
rmc-ert-admin-tab-requests = Requests
rmc-ert-admin-tab-force = Force Call
rmc-ert-admin-no-requests = No ERT requests are active.
rmc-ert-admin-force-no-calls = No ERT calls are available.
rmc-ert-admin-force-reason = Reason:
rmc-ert-admin-force-row-category = Category: {$category}
rmc-ert-admin-force-row-id = Prototype: {$id}
rmc-ert-admin-force-row-organization = Organization: {$organization}
rmc-ert-admin-force-row-compact-meta = Prototype: {$id} | Organization: {$organization}
rmc-ert-admin-row-summary = {$state} | {$source} | {$requester} via {$sourceName} | {$createdAt}
rmc-ert-admin-row-reason = Reason: {$reason}
rmc-ert-admin-row-selected = Selected: {$call}
rmc-ert-admin-row-error = Error: {$error}
rmc-ert-admin-row-warning = Warning: {$warning}
rmc-ert-admin-card-created = Created: {$time}
rmc-ert-admin-card-requester = Requester:
rmc-ert-admin-card-source = Source:
rmc-ert-admin-card-via = Via:
rmc-ert-admin-card-selected = Call:
rmc-ert-admin-card-reason = Reason:
rmc-ert-admin-card-error = Error:
rmc-ert-admin-card-warning = Warning:
rmc-ert-admin-card-actions = Actions
rmc-ert-admin-card-no-actions = No actions available
rmc-ert-admin-card-none = -
rmc-ert-admin-action-approve-random = Approve Random
rmc-ert-admin-action-deny = Deny
rmc-ert-admin-action-send = Send {$call}
rmc-ert-admin-action-launch = Launch
rmc-ert-admin-action-cancel = Cancel
rmc-ert-admin-action-complete = Complete
rmc-ert-admin-action-force-call = Force Call
rmc-ert-admin-action-force-call-short = Call

rmc-ert-source-console = Console
rmc-ert-source-handheld = Handheld
rmc-ert-source-admin = Admin
rmc-ert-source-ares = ARES

rmc-ert-state-requested = Requested
rmc-ert-state-pending-admin = Pending Admin
rmc-ert-state-pending-dispatch = Pending Dispatch
rmc-ert-state-recruiting = Recruiting
rmc-ert-state-spawning = Spawning
rmc-ert-state-launching = Launching
rmc-ert-state-arrived = Arrived
rmc-ert-state-completed = Completed
rmc-ert-state-denied = Denied
rmc-ert-state-cancelled = Cancelled
rmc-ert-state-failed = Failed

rmc-ert-popup-beacon-spent = The distress beacon has already been used.
rmc-ert-popup-beacon-cooldown = The distress beacon is still recalibrating.
rmc-ert-popup-beacon-reason-required = You need to provide a reason before transmitting the distress beacon.
rmc-ert-popup-console-unavailable = This console cannot transmit a distress beacon.
rmc-ert-popup-no-source-teams = There are no configured response teams for this distress source.
rmc-ert-prompt-console-reason = State the reason for the distress beacon.
rmc-ert-prompt-handheld-reason = State the reason for the {$title}.
rmc-ert-console-request-announcement = A distress beacon has been launched. High Command is reviewing the request.

rmc-ert-admin-actor-server = server
rmc-ert-launcher-automatic = automatic launch timer
rmc-ert-response-team-fallback = response team
rmc-ert-cleanup-reason-cancelled = cancelled
rmc-ert-cleanup-reason-failed = failed
rmc-ert-cleanup-reason-fallback-return = returned to fallback base

rmc-ert-admin-approved = {$admin} approved ERT request {$id} as {$call}. Dispatching in {$delay} seconds.
rmc-ert-admin-denied = {$admin} denied ERT request {$id} from {$requester}.
rmc-ert-admin-cancelled = {$admin} cancelled ERT request {$id}.
rmc-ert-admin-completed = {$admin} completed ERT request {$id} for {$team}.
rmc-ert-admin-arrived-missing-call = ERT request {$id} arrived, but its call prototype is no longer available.
rmc-ert-admin-recruiting = ERT request {$id} is recruiting {$slots} ghost-role slots for {$call}.
rmc-ert-admin-cleanup = ERT request {$id} {$reason}; shuttle content cleaned up.
rmc-ert-admin-cleanup-deferred = ERT request {$id} {$reason}; shuttle cleanup deferred because {$actors} actor(s) remain aboard.
rmc-ert-admin-launched = ERT request {$id} for {$call} launched by {$launcher}.
rmc-ert-admin-failed = ERT request {$id} failed: {$error}
rmc-ert-admin-request = ERT request {$id} from {$requester} via {$source}: {$reason}
rmc-ert-admin-request-with-extra = {$base} {$extra}
rmc-ert-admin-arrived = ERT request {$id} for {$call} arrived.
rmc-ert-admin-arrived-detail = ERT request {$id} for {$call} arrived via {$detail}.
rmc-ert-admin-force-called = {$admin} forced ERT request {$id} as {$call}. Dispatching in {$delay} seconds.
rmc-ert-admin-force-called-reason = {$admin} forced ERT request {$id} as {$call}. Dispatching in {$delay} seconds. Reason: {$reason}

rmc-ert-success-handheld = A distress beacon request has been sent to {$recipient}.
rmc-ert-success-console = The distress beacon has been transmitted to High Command.

rmc-ert-briefing-title = {$team} briefing
rmc-ert-briefing-reason = Reason: {$reason}
rmc-ert-briefing-objectives = Objectives:
rmc-ert-briefing-bullet = - 
rmc-ert-briefing-features = Operational notes:
rmc-ert-briefing-role = Assigned role: {$role}

rmc-ert-error-unknown-call = Unknown ERT call prototype: {$id}
rmc-ert-error-call-not-allowed = {$call} is not allowed for this distress source.
rmc-ert-error-console-random-only = Console distress requests can only be approved as a random response team.
rmc-ert-error-call-disabled = {$call} is disabled.
rmc-ert-error-selected-call-missing = The selected ERT call no longer exists.
rmc-ert-error-load-shuttle-map = Failed to load ERT shuttle map {$map}.
rmc-ert-error-missing-ghost-role = Missing ghost role entity prototype {$entity} for {$call}.
rmc-ert-error-min-slots-over-max = ERT call {$call} requires at least {$required} slots, but its configured maximum is {$maximum}.
rmc-ert-error-planned-slots-too-low = Only {$planned} ERT slots were planned, but {$required} are required.
rmc-ert-error-unknown-shuttle-spawner = Unknown ERT shuttle spawner prototype {$id}.
rmc-ert-error-shuttle-spawner-missing-grid = ERT shuttle spawner {$id} is missing GridSpawnerComponent.
rmc-ert-error-shuttle-spawner-no-map = ERT shuttle spawner {$id} does not define a shuttle map.
rmc-ert-error-beacon-no-teams = The beacon cannot find any configured response teams.
rmc-ert-error-raffles-in-progress = Ghost role raffles are still in progress for this response team.
rmc-ert-error-no-volunteers = No volunteers accepted the emergency response deployment.
rmc-ert-error-not-enough-volunteers = Only {$accepted} emergency responders accepted deployment, but {$required} are required.
rmc-ert-arrived-detail-no-shuttle = {$launcher} without a shuttle
rmc-ert-error-no-navigation-computer = The ERT shuttle has no navigation computer.
rmc-ert-error-no-landing-zone = No valid ERT landing zone is available.
rmc-ert-error-launch-failed = The ERT shuttle failed to launch.
rmc-ert-error-docking-verification-failed = The ERT shuttle failed to dock with the selected landing port.
rmc-ert-warning-no-compatible-landing-zone = {$call} cannot be approved: no compatible landing zone or docking port is available.
rmc-ert-error-no-random-calls = No weighted random ERT calls are available for this request.
rmc-ert-error-random-selection-failed = Weighted random ERT selection failed.
rmc-ert-error-unavailable-evacuation = {$call} is unavailable while evacuation is in progress.
rmc-ert-error-unavailable-hijack = {$call} is unavailable during hijack conditions.
rmc-ert-error-min-round-time = {$call} requires at least {$minutes} minutes of round time.
rmc-ert-error-max-calls-reached = {$call} has already been dispatched this round.
rmc-ert-error-source-cooldown = {$call} is on cooldown for this distress source.
rmc-ert-error-source-pending = A distress request from this source is already pending.
rmc-ert-error-call-not-force-callable = {$call} is not available for force calling.
rmc-ert-error-force-call-failed = Failed to force call {$call}.

rmc-ert-category-response = Response
rmc-ert-category-military = Military
rmc-ert-category-corporate = Corporate
rmc-ert-category-law = Law
rmc-ert-category-foreign-military = Foreign Military
rmc-ert-category-event = Event

rmc-ert-organization-unmc = UNMC
rmc-ert-organization-weya = WeYa
rmc-ert-organization-cmb = CMB
rmc-ert-organization-spp = SPP
rmc-ert-organization-tse = TSE
rmc-ert-organization-provost = Provost
rmc-ert-organization-clf = CLF
rmc-ert-organization-pizza = Pizza Galaxy

rmc-ert-admin-button-cbrn = SEND CBRN
rmc-ert-admin-button-pmc = SEND PMC
rmc-ert-admin-button-bodyguards = SEND BODYGUARDS
rmc-ert-admin-button-lawyers = SEND LAWYERS
rmc-ert-admin-button-cmb = SEND CMB
rmc-ert-admin-button-spp = SEND SPP
rmc-ert-admin-button-tse = SEND TSE
rmc-ert-admin-button-provost = SEND PROVOST
rmc-ert-admin-button-clf = SEND CLF
rmc-ert-admin-button-pizza = SEND PIZZA

rmc-ert-call-cbrn-name = UNMC CBRN Response Team
rmc-ert-call-pmc-name = WeYa PMC Response Team
rmc-ert-call-weya-bodyguard-name = WeYa Executive Protection Detail
rmc-ert-call-weya-lawyers-name = WeYa Corporate Affairs Team
rmc-ert-call-cmb-name = CMB Marshal Patrol
rmc-ert-call-spp-name = SPP Response Squad
rmc-ert-call-tse-name = TSE Royal Marines
rmc-ert-call-provost-name = Provost Enforcement Team
rmc-ert-call-clf-name = CLF Cell
rmc-ert-call-pizza-name = Pizza Galaxy Delivery Shuttle

rmc-ert-role-cbrn-leader = CBRN Squad Leader
rmc-ert-role-cbrn-rifleman = CBRN Rifleman
rmc-ert-role-cbrn-medic = CBRN Hospital Corpsman
rmc-ert-role-cbrn-engineer = CBRN Combat Technician
rmc-ert-role-pmc-leader = PMC Leader
rmc-ert-role-pmc-operator = PMC Operator
rmc-ert-role-pmc-medic = PMC Medic
rmc-ert-role-pmc-engineer = PMC Engineer
rmc-ert-role-weya-bodyguard-lead = Executive Protection Lead
rmc-ert-role-weya-bodyguard = Executive Bodyguard
rmc-ert-role-weya-lawyer-supervisor = Corporate Executive Supervisor
rmc-ert-role-weya-lawyer-specialist = Corporate Executive Specialist
rmc-ert-role-cmb-marshal = CMB Marshal
rmc-ert-role-cmb-deputy = CMB Deputy
rmc-ert-role-spp-leader = SPP Squad Leader
rmc-ert-role-spp-rifleman = SPP Rifleman
rmc-ert-role-spp-medic = SPP Medic
rmc-ert-role-tse-teamlead = Royal Marines Team Leader
rmc-ert-role-tse-commando = Royal Marines Commando
rmc-ert-role-tse-medic = Royal Marines Medic
rmc-ert-role-provost-leader = Provost Team Leader
rmc-ert-role-provost-enforcer = Provost Enforcer
rmc-ert-role-clf-leader = CLF Cell Leader
rmc-ert-role-clf-soldier = CLF Soldier
rmc-ert-role-clf-medic = CLF Medic
rmc-ert-role-pizza-deliverer = Pizza Deliverer

rmc-ert-announcement-recruiting-immediate = {$team} is accepting volunteers for immediate deployment.
rmc-ert-announcement-recruiting = {$team} is accepting volunteers.
rmc-ert-announcement-launch = {$team} has launched.

rmc-ert-announcement-cbrn-dispatch = High Command has approved a distress response. {$team} is being mustered.
rmc-ert-announcement-cbrn-arrival = {$team} has docked aboard the warship and is awaiting tasking.
rmc-ert-announcement-cbrn-failed = High Command reports that {$team} deployment failed: {$reason}
rmc-ert-announcement-cbrn-denied = High Command has denied the distress request.

rmc-ert-announcement-pmc-dispatch = A corporate emergency contract has been accepted. {$team} is preparing to deploy.
rmc-ert-announcement-pmc-arrival = {$team} has docked aboard the warship under corporate authority.
rmc-ert-announcement-pmc-denied = Corporate response channels have declined the request.

rmc-ert-announcement-bodyguard-dispatch = Corporate security has accepted a close-protection request. {$team} is preparing to deploy.
rmc-ert-announcement-bodyguard-arrival = {$team} has docked aboard the warship and is moving to secure the principal.
rmc-ert-announcement-bodyguard-denied = Corporate security declined the executive protection request.

rmc-ert-announcement-lawyers-dispatch = Corporate Affairs has acknowledged the distress request. {$team} is preparing to deploy.
rmc-ert-announcement-lawyers-arrival = {$team} has docked aboard the warship and is preparing to review contractual violations.
rmc-ert-announcement-lawyers-denied = Corporate Affairs declined the legal assistance request.

rmc-ert-announcement-cmb-dispatch = A Colonial Marshal Bureau patrol has accepted the distress request.
rmc-ert-announcement-cmb-arrival = {$team} has docked aboard the warship and is investigating the reported incident.

rmc-ert-announcement-spp-dispatch = Foreign response traffic has been detected. {$team} is preparing to deploy.
rmc-ert-announcement-spp-arrival = {$team} has docked aboard the warship and is preparing to support the operation.

rmc-ert-announcement-tse-dispatch = {$team} has accepted the distress request.
rmc-ert-announcement-tse-arrival = {$team} has docked aboard the warship and is entering the operation area.

rmc-ert-announcement-provost-dispatch = Provost command has approved a response team.
rmc-ert-announcement-provost-arrival = {$team} has docked aboard the warship and is assuming Provost jurisdiction.

rmc-ert-announcement-pizza-dispatch = Pizza Galaxy has accepted the emergency delivery order. {$team} is preparing to deploy.
rmc-ert-announcement-pizza-arrival = 'That'll be... sixteen orders of cheesy fries, eight large double topping pizzas, nine bottles of Four Loko... hello? Is anyone on this ship? Your pizzas are getting cold.'

rmc-ert-beacon-request-title-handheld = handheld distress beacon
rmc-ert-beacon-request-title-cmb = CMB distress beacon
rmc-ert-beacon-request-title-weya = WeYa distress beacon
rmc-ert-beacon-request-title-weya-bodyguard = WeYa bodyguard distress beacon
rmc-ert-beacon-request-title-weya-lawyer = WeYa legal distress beacon
rmc-ert-beacon-request-title-provost = Provost distress beacon
rmc-ert-beacon-request-title-foreign = foreign distress beacon

rmc-ert-recipient-high-command = High Command
rmc-ert-recipient-anchorpoint = Anchorpoint Station
rmc-ert-recipient-weya-command = Weyland-Yutani emergency response command
rmc-ert-recipient-weya-security = Weyland-Yutani Corporate Security Division
rmc-ert-recipient-weya-affairs = Weyland-Yutani Corporate Affairs Division
rmc-ert-recipient-provost = Provost command
rmc-ert-recipient-allied = allied response command
rmc-ert-recipient-pizza = Pizza Galaxy dispatch

rmc-ert-beacon-request-title-pizza = pizza delivery distress beacon

rmc-ert-objective-pizza-tip = Make sure you get a tip!
rmc-ert-feature-pizza-wrong-place = You're PRETTY sure this is the right place.
