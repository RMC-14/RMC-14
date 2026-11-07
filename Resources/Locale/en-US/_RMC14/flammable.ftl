rmc-immune-to-ignition-examine = [color=cyan]{CAPITALIZE(SUBJECT($ent))} can't be {$direct ->
    [true]{""}
    *[false]{"indirectly "}
    }ignited![/color]
rmc-immune-to-fire-tile-damage-examine = [color=cyan]{CAPITALIZE(SUBJECT($ent))} takes no damage from tile fires![/color]
rmc-fire-armor-debuff-modifier-examine = [color=cyan]{CAPITALIZE(SUBJECT($ent))} has {POSS-ADJ($ent)} armor reduced {$percentage}% less when standing on green fire![/color]

rmc-flammable-molotov-examine = [color=cyan]You can turn this into a molotov with a piece of paper![/color]
rmc-flammable-pat-fire-user = You try to put out the fire on {$target}!
rmc-flammable-pat-fire-target = {$user} tries to put out the fire on you!
rmc-flammable-pat-fire-others = {$user} tries to put out the fire on {$target}!
rmc-flammable-empty = The {$item} is empty...

rmc-flammable-not-enough-liquid = There's not enough flammable liquid in the {$container}!
