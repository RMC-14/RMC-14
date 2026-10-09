rmc-dual-tube-must-hold = You must be holding {THE($gun)} in your active hand to switch the active internal magazine!
rmc-dual-tube-overloaded = The current tube is overloaded! {CAPITALIZE(THE($gun))} spits out the chambered shell!
rmc-dual-tube-chamber-swap-on = You will start swapping the chambered shell with the other tube. Your current tube must be underloaded or it will forcefully eject the shell out of the chamber.
rmc-dual-tube-chamber-swap-off = You will stop swapping the chambered shell with the other tube.
rmc-dual-tube-verb = Toggle shotgun tube

rmc-dual-tube-examine-tube = The [bold]{ $second ->
    [true] second
   *[false] first
}[/bold] tube is active. The other tube holds [color=cyan]{$count}[/color] { $count ->
    [one] shell
   *[other] shells
}.
rmc-dual-tube-examine-chamber-swap = Use your [color=cyan]cycle firemode[/color] keybind to toggle chamber-swapping. Chamber-swapping is [bold]{ $active ->
    [true] on
   *[false] off
}[/bold].
