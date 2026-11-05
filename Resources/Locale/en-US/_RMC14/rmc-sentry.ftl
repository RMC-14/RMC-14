rmc-sentry-need-open-area = You need a clear, open area to build a {$sentry}, something is blocking the way in front of you!
rmc-sentry-on = {CAPITALIZE(THE($sentry))} hums to life and emits several beeps.
rmc-sentry-off = {CAPITALIZE(THE($sentry))} powers down and goes silent.
rmc-sentry-magazine-invalid = {CAPITALIZE(THE($item))} isn't a valid sentry magazine!
rmc-sentry-magazine-swap-not-empty = You can't swap the magazine until it's empty.
rmc-sentry-magazine-swap-start-user = You begin swapping a new {$magazine} into {THE($sentry)}.
rmc-sentry-magazine-swap-start-others = {$user} begins swapping a new {$magazine} into {THE($sentry)}.
rmc-sentry-magazine-swap-finish-user = You swap a new {$magazine} into {THE($sentry)}.
rmc-sentry-magazine-swap-finish-others = {$user} swaps a new {$magazine} into {THE($sentry)}.
rmc-sentry-limited-rotation = [color=red]It can only turn a maximum of {$degrees} degrees when firing.[/color]
rmc-sentry-disassembled-with-multitool = It can be disassembled with a [color=cyan]multitool[/color].
rmc-sentry-rotate-with-screwdriver = It can be rotated with a [color=cyan]screwdriver[/color].
rmc-sentry-too-close = This is too close to {INDEFINITE($defense)} {$defense}!
rmc-sentry-active-norot = {CAPITALIZE(THE($sentry))} is currently active. The motors will prevent you from rotating it safely.
rmc-sentry-item-norot = You can't rotate it like this!
rmc-sentry-magazine-does-not-fit = {CAPITALIZE(THE($magazine))} doesn't fit into {THE($sentry)}!

rmc-sentry-upgrade-not-item = You need to disassemble {THE($sentry)} with a multitool before upgrading it!
rmc-sentry-upgrade-not-holding = You need to be holding an upgrade kit in your active hand to upgrade {THE($sentry)}!
rmc-sentry-upgrade-not-upgradeable = You can't upgrade {THE($sentry)}!

rmc-sentry-spikes-self = The sentry's steel tusks cut into you!
rmc-sentry-spikes-others = The sentry's steel tusks cut into {$target}!

rmc-sentry-rotate-self = You rotate {THE($sentry)}.
rmc-sentry-rotate-others = {$user} rotates {THE($sentry)}.

rmc-sentry-disassemble-start-self = You begin disassembling {THE($sentry)}.
rmc-sentry-disassemble-start-others = {$user} begins disassembling {THE($sentry)}.
rmc-sentry-disassemble-finish-self = You disassemble {THE($sentry)}.
rmc-sentry-disassemble-finish-others = {$user} disassembles {THE($sentry)}.

rmc-sentry-unanchor-is-on = {CAPITALIZE(THE($sentry))} is currently active. The motors will prevent you from unanchoring it safely.

rmc-sentry-not-emergency = {CAPITALIZE(THE($deployer))} can only be activated in emergencies.
rmc-sentry-deploy = You deploy {THE($spawned)}

### Sentry laptop
rmc-sentry-laptop-title = Sentry Network Control
rmc-sentry-laptop-header = [color=#88C7FA][font size=16][bold]SENTRY NETWORK - {$count}/{$max} LINKED[/bold][/font][/color]
rmc-sentry-laptop-status-none = Status: No sentries linked
rmc-sentry-laptop-status-count = Status: {$online}/{$total} Online
rmc-sentry-laptop-card-default-name = [color=#88C7FA][bold]UN 571-C Sentry Gun[/bold][/color]
rmc-sentry-laptop-status-online = [color=#229132][ONLINE][/color]
rmc-sentry-laptop-status-offline = [color=#CED22B][OFFLINE][/color]
rmc-sentry-laptop-status-packed = [color=#A42625][OFFLINE][/color]
rmc-sentry-laptop-name = NAME:
rmc-sentry-laptop-name-placeholder = Enter custom name...
rmc-sentry-laptop-health = HEALTH
rmc-sentry-laptop-ammo = AMMO
rmc-sentry-laptop-mode = MODE
rmc-sentry-laptop-mode-active = [color=#229132]Active[/color]
rmc-sentry-laptop-mode-standby = [color=#CED22B]Standby[/color]
rmc-sentry-laptop-mode-packed = [color=#A42625]Packed[/color]
rmc-sentry-laptop-location = LOCATION
rmc-sentry-laptop-unknown = Unknown
rmc-sentry-laptop-target = TARGET
rmc-sentry-laptop-target-none = [color=#B0B0B0]None[/color]
rmc-sentry-laptop-iff-controls = IFF TARGETING CONTROLS
rmc-sentry-laptop-reset-default = RESET TO DEFAULT
rmc-sentry-laptop-toggle-power = TOGGLE POWER
rmc-sentry-laptop-view-cam = VIEW CAM
rmc-sentry-laptop-unlink = UNLINK
rmc-sentry-laptop-select-all = Select All
rmc-sentry-laptop-deselect-all = Deselect All
rmc-sentry-laptop-global-iff = GLOBAL IFF
rmc-sentry-laptop-refresh = REFRESH
rmc-sentry-laptop-unlink-all = UNLINK ALL
rmc-sentry-laptop-search = SEARCH:
rmc-sentry-laptop-search-placeholder = Search sentries by name or location...
rmc-sentry-laptop-global-iff-controls = GLOBAL IFF TARGETING CONTROLS
rmc-sentry-laptop-apply-all = APPLY TO ALL
rmc-sentry-laptop-reset-all-default = RESET ALL TO DEFAULT
rmc-sentry-laptop-power-all-on = POWER ALL ON
rmc-sentry-laptop-power-all-off = POWER ALL OFF
rmc-sentry-laptop-camera-view = [color=#88C7FA][bold]CAMERA VIEW[/bold][/color]
rmc-sentry-laptop-camera-title = [color=#88C7FA][bold]CAMERA: {$name} (Range: {$range} tiles, FOV: {$fov})[/bold][/color]
rmc-sentry-laptop-close = CLOSE

### Sentry laptop popups
rmc-sentry-laptop-place-on-table = Place the laptop on a table first!
rmc-sentry-laptop-must-be-open = The laptop must be opened first!
rmc-sentry-laptop-max-sentries = The laptop can only control {$max} sentries at once!
rmc-sentry-laptop-linked = Successfully linked {$sentry} to the laptop.

rmc-sentry-laptop-alert-low-ammo = {$sentry}: LOW AMMO ({$ammo}/{$maxAmmo})
rmc-sentry-laptop-alert-critical-damage = {$sentry}: CRITICAL DAMAGE
rmc-sentry-laptop-alert-damaged = {$sentry}: Taking damage! ({$percent}% health)
rmc-sentry-laptop-alert-engaging = {$sentry}: Engaging {$target}
