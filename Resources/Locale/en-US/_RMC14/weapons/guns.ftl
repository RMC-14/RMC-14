cm-gun-unskilled = You don't seem to know how to use {THE($gun)}
cm-gun-no-ammo-message = You don't have any ammo left!
cm-gun-use-delay = You need to wait {$seconds} seconds before shooting again!
cm-gun-pump-examine = [bold]Press your [color=cyan]unique action[/color] keybind (Spacebar by default) to pump before shooting.[/bold]
cm-gun-pump-first-with = You need to pump the gun with {$key} first!
cm-gun-pump-first = You need to pump the gun first!

rmc-breech-loaded-open-shoot-attempt = You need to close the breech first!
rmc-breech-loaded-not-ready-to-shoot = You need to open and close the breech first!
rmc-breech-loaded-closed-load-attempt = You need to open the breech first!
rmc-breech-loaded-closed-extract-attempt = You need to open the breech first!

rmc-wield-use-delay = You need to wait {$seconds} seconds before wielding {THE($wieldable)}!
rmc-shoot-use-delay = You need to wait {$seconds} seconds before shooting {THE($wieldable)}!

rmc-shoot-harness-required = Harness required
rmc-wear-smart-gun-required = You must have your smart gun equipped to wear these.

rmc-shoot-id-lock-unauthorized = Trigger locked. Unauthorized user.
rmc-id-lock-unauthorized = Action denied. Unauthorized user.
rmc-id-lock-authorization = You pick up the {$gun}, registering yourself as its owner.
rmc-id-lock-authorization-combat = The {$gun} beeps, registering yourself as its owner.
rmc-id-lock-toggle-lock = You {$action} the id lock on the {$gun}.

rmc-id-lock-color-unauthorized = red
rmc-id-lock-color-authorized = chartreuse
rmc-id-lock-toggle-on = lock
rmc-id-lock-toggle-off = unlock

rmc-iff-toggle = You {$action} the IFF on the {$gun}.
rmc-iff-toggle-off = disable
rmc-iff-toggle-on = enable

rmc-revolver-spin = You spin the cylinder.

rmc-examine-text-weapon-accuracy = The current accuracy multiplier is [color={$colour}]{TOSTRING($accuracy, "F2")}[/color].

rmc-examine-text-scatter-max = Current maximum scatter is [color={$colour}]{TOSTRING($scatter, "F1")}[/color] degrees.
rmc-examine-text-scatter-min = Current minimum scatter is [color={$colour}]{TOSTRING($scatter, "F1")}[/color] degrees.
rmc-examine-text-shots-to-max-scatter = It takes [color={$colour}]{$shots}[/color] shots to reach maximum scatter.
rmc-examine-text-iff = [color=cyan]This gun will ignore and shoot past friendlies![/color]
rmc-examine-text-id-lock-no-user = [color=chartreuse]It's unregistered. Pick it up to register yourself as its owner.[/color]
rmc-examine-text-id-lock = [color=chartreuse]It is registered to [/color][color={$color}]{$name}[/color][color=chartreuse].[/color]
rmc-examine-text-id-lock-unlocked = [color=chartreuse]It is registered to [/color][color={$color}]{$name}[/color][color=chartreuse], but has its fire restrictions unlocked.[/color]

rmc-gun-rack-examine = [bold]Press your [color=cyan]unique action[/color] keybind (Spacebar by default) to rack before shooting.[/bold]
rmc-gun-rack-first-with = You need to rack the gun with {$key} first!
rmc-gun-rack-first = You need to rack the gun first!

rmc-assisted-reload-fail-angle = You must be standing behind {$target} in order to reload {POSS-ADJ($target)} weapon!
rmc-assisted-reload-fail-full = {CAPITALIZE(POSS-ADJ($target))} {$weapon} is already loaded.
rmc-assisted-reload-fail-mismatch = The {$ammo} can't be loaded into a {$weapon}!
rmc-assisted-reload-start-user = You begin reloading {$target}'s {$weapon}! Hold still...
rmc-assisted-reload-start-target = {$reloader} begins reloading your {$weapon} with the {$ammo}! Hold still...

rmc-gun-stacks-hit-single = Bullseye!
rmc-gun-stacks-hit-multiple = Bullseye! {$hits} hits in a row!
rmc-gun-stacks-reset = The {$weapon} beeps as it loses its targeting data, and returns to normal firing procedures.

rmc-gun-shoot-air-self = YOU FIRE YOUR { CAPITALIZE($weapon) } INTO THE AIR!
rmc-gun-shoot-air-other = { CAPITALIZE(THE($user)) } FIRES { CAPITALIZE(THE($weapon)) } INTO THE AIR!
rmc-gun-shoot-air-blocked = The roof above you is too dense.

rmc-flare-gun-examine = The last signal flare fired has the designation: [color=#ad3b98][bold]{$id}[/bold][/color]
