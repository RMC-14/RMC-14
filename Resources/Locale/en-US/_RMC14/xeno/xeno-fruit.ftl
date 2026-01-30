# Fruit choosing
rmc-xeno-fruit-choose = We will now plant {$fruit} when secreting resin.

# Plant failures
rmc-xeno-fruit-plant-failed = We can't plant there!
rmc-xeno-fruit-plant-failed-weeds = We cannot plant a fruit without a weed garden!
rmc-xeno-fruit-plant-failed-select = You need to select a fruit to plant first! Use the "Choose Resin Fruit" action.
rmc-xeno-fruit-plant-failed-hive = These weeds do not belong to our hive; they reject our fruit.
rmc-xeno-fruit-plant-failed-resin-hole = This location is too close to a resin hole!
rmc-xeno-fruit-plant-failed-fruit = This location is too close to another fruit!
rmc-xeno-fruit-plant-failed-node = There is already a resin node here!

# Plant success
rmc-xeno-fruit-plant-limit-exceeded = We cannot sustain another fruit, one will wither away to allow this one to live!
rmc-xeno-fruit-plant-success-self = We secrete a portion of our vital fluids and shape them into a fruit!
rmc-xeno-fruit-plant-success-others = {CAPITALIZE(THE($xeno))} secretes fluids and shapes them into a fruit!

# Harvest
rmc-xeno-fruit-crush = We crush {THE($fruit)}.
rmc-xeno-fruit-harvest-start-xeno = We start uprooting {THE($fruit)}...
rmc-xeno-fruit-harvest-start-marine = You start uprooting {THE($fruit)}...
rmc-xeno-fruit-harvest-failed-xeno = We can't harvest that!
rmc-xeno-fruit-harvest-failed-marine = You can't harvest that!
rmc-xeno-fruit-harvest-failed-too-small = We are too small to pick up {THE($fruit)}!
rmc-xeno-fruit-harvest-failed-not-mature-xeno = {CAPITALIZE(THE($fruit))} disintegrates in our hands as we uproot it.
rmc-xeno-fruit-harvest-failed-not-mature-marine = {CAPITALIZE(THE($fruit))} disintegrates in your hands as you uproot it.
rmc-xeno-fruit-harvest-success-xeno = We uproot {THE($fruit)}.
rmc-xeno-fruit-harvest-success-marine = You uproot {THE($fruit)}.

# Fruit consuming (from ground)
rmc-xeno-fruit-pick-prepare = We prepare to consume {THE($fruit)}.
rmc-xeno-fruit-pick-failed-already = {CAPITALIZE(THE($fruit))} is already being picked!
rmc-xeno-fruit-pick-failed-no-longer = We can no longer consume {THE($fruit)}.
rmc-xeno-fruit-pick-failed-not-mature = {CAPITALIZE(THE($fruit))} isn't ripe yet. We need to wait a little longer.
rmc-xeno-fruit-pick-failed-health-full = We are at full health! This would be a waste...
rmc-xeno-fruit-pick-failed-health-full-target = She is at full health! This would be a waste...

# Fruit eating
rmc-xeno-fruit-eat-start-self = We start eating {THE($fruit)}.
rmc-xeno-fruit-eat-start-others = {CAPITALIZE(THE($xeno))} starts eating {THE($fruit)}.

rmc-xeno-fruit-eat-fail-self = We fail to eat {THE($fruit)}.
rmc-xeno-fruit-eat-fail-others = {CAPITALIZE(THE($xeno))} fails to eat {THE($fruit)}.

rmc-xeno-fruit-eat-success-self = We ate {THE($fruit)}.
rmc-xeno-fruit-eat-success-others = {CAPITALIZE(THE($xeno))} ate {THE($fruit)}.

# Fruit feeding
rmc-xeno-fruit-feed-refuse = {CAPITALIZE(THE($target))} refuses to eat {THE($fruit)}.
rmc-xeno-fruit-feed-dead = {CAPITALIZE(THE($target))} is already dead, she won't benefit from the fruit now...

rmc-xeno-fruit-feed-start-self = We start feeding {THE($target)} {THE($fruit)}.
rmc-xeno-fruit-feed-start-target = {CAPITALIZE(THE($user))} starts feeding us {THE($fruit)}.
rmc-xeno-fruit-feed-start-others = {CAPITALIZE(THE($user))} starts feeding {THE($target)} {THE($fruit)}.

rmc-xeno-fruit-feed-fail-self = We fail to feed {THE($target)} {THE($fruit)}.
rmc-xeno-fruit-feed-fail-target = {CAPITALIZE(THE($user))} fails to feed us {THE($fruit)}.
rmc-xeno-fruit-feed-fail-others = {CAPITALIZE(THE($user))} fails to feed {THE($target)} {THE($fruit)}.

rmc-xeno-fruit-feed-success-self = We fed {THE($target)} {THE($fruit)}.
rmc-xeno-fruit-feed-success-target = {CAPITALIZE(THE($user))} fed us {THE($fruit)}.
rmc-xeno-fruit-feed-success-others = {CAPITALIZE(THE($user))} fed {THE($target)} {THE($fruit)}.

# Fruit removed
rmc-xeno-fruit-destroyed = We sense one of our fruits has been destroyed!
rmc-xeno-fruit-consumed = One of our resin fruits has been consumed.
rmc-xeno-fruit-picked = One of our resin fruits has been picked.

# Fruit effect pop-ups
rmc-xeno-fruit-effect-lesser = We recover a bit from our injuries.
rmc-xeno-fruit-effect-greater = We recover a bit from our injuries, and begin to regenerate rapidly.
rmc-xeno-fruit-effect-unstable = We feel our defense being bolstered, and begin to regenerate rapidly.
rmc-xeno-fruit-effect-spore = We feel a frenzy coming onto us! Our abilities will cool off faster as we slash!
rmc-xeno-fruit-effect-speed = The fruit invigorates us to move faster!
rmc-xeno-fruit-effect-plasma = The fruit boosts our plasma regeneration!
rmc-xeno-fruit-effect-already = We are already under the effects of {THE($fruit)}, go out and kill!
rmc-xeno-fruit-effect-already-feed = {CAPITALIZE(THE($xeno))} is already under the effects of {THE($fruit)}!
rmc-xeno-fruit-effect-end = We feel the effects of the fruit wane...

# Verbs
rmc-xeno-fruit-verb-harvest = Harvest
rmc-xeno-fruit-verb-consume = Consume
rmc-xeno-fruit-verb-feed = Feed

# Examine text
rmc-xeno-fruit-examine-base = This fruit is {$growthStatus}.
rmc-xeno-fruit-examine-growing = still [color=darkred]growing[/color]
rmc-xeno-fruit-examine-grown = [color=darkgreen]fully grown[/color]
rmc-xeno-fruit-examine-spent = [color=orange]spent[/color]

# UI text
rmc-xeno-fruit-ui-title = Choose Resin Fruit
rmc-xeno-fruit-ui-count = Planted fruit: {$count}/{$max}

# Not-same hive
rmc-xeno-fruit-wrong-hive = This isn't from our hive!
rmc-xeno-fruit-feed-wrong-hive = {THE($target)} isn't from our hive!

# Weeds
rmc-xeno-fruit-weed-boost = We sense that these weeds multiply resin fruit growth time by [bold]{$percent}%[/bold].

# Effects
rmc-xeno-fruit-consume-examine = This fruit gives the following effects on consumption:
rmc-xeno-fruit-instant-heal = Instantly restores [bold]{$amount}[/bold] health.
rmc-xeno-fruit-regen-heal = Regenerates [bold]{$amount}[/bold] health per second for {$time} seconds.
rmc-xeno-fruit-shield = Grants an overshield equal to [bold]{$percent}%[/bold] of our max health, up to [bold]{$max}[/bold] max. It decays after {$duration} seconds, losing {$decay} per second.
rmc-xeno-fruit-cooldown = Reduces ability cooldowns on next cast by [bold]{$amount}%[/bold] on slash, up to [bold]{$max}%[/bold] max. This effect lasts {$time} seconds.
rmc-xeno-fruit-speed = Boosts our speed by [bold]{$amount}[/bold] for {$time} seconds.
rmc-xeno-fruit-regen-plasma = Regenerates [bold]{$amount}[/bold] plasma per second for {$time} seconds.
