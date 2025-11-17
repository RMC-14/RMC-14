command-cfsetroundseed-description = Queue a seed for the next round (lobby only).
command-cfsetroundseed-help = Usage: cfsetroundseed <seed>
command-cfsetroundseed-usage = Usage: cfsetroundseed <seed>
command-cfsetroundseed-not-in-lobby = Command input is only available in the lobby.
command-cfsetroundseed-success = Seed "{$seed}" will be used for the next round.
command-cfsetroundseed-server-console = Server console

command-cfgetroundseed-description = Show the current round seed.
command-cfgetroundseed-help = Usage: cfgetroundseed
command-cfgetroundseed-no-seed = Seed is not available yet.
command-cfgetroundseed-success = Current round seed: {$seed}

round-seed-system-actor-server = Server
round-seed-system-log-queued-next = {$by} queued seed "{$seedText}" (value {$seedValue}) for the next round
round-seed-system-log-seed-queued = Round {$roundId} seed queued: "{$seedText}" -> {$seedValue}
round-seed-system-log-seed-generated = Round {$roundId} seed generated: {$seedValue}

command-cfgetdayphase-description = Show the current day phase for a map.
command-cfgetdayphase-help = Usage: cfgetdayphase <mapId>
command-cfgetdayphase-usage = Usage: cfgetdayphase <mapId>
command-cfgetdayphase-invalid-map = Invalid map id.
command-cfgetdayphase-no-cycle = No day/night cycle is active on this map.
command-cfgetdayphase-success = Current day phase: {$phase} (day {$day})

command-cfgetdaytime-description = Show the current in-world time (HH:MM) for a map.
command-cfgetdaytime-help = Usage: cfgetdaytime <mapId>
command-cfgetdaytime-usage = Usage: cfgetdaytime <mapId>
command-cfgetdaytime-invalid-map = Invalid map id.
command-cfgetdaytime-no-cycle = No day/night cycle is active on this map.
command-cfgetdaytime-success = Current in-world time: {$time} (day {$day})

command-cfgetdayinfo-description = Show current day, phase and time for a map.
command-cfgetdayinfo-help = Usage: cfgetdayinfo <mapId>
command-cfgetdayinfo-usage = Usage: cfgetdayinfo <mapId>
command-cfgetdayinfo-invalid-map = Invalid map id.
command-cfgetdayinfo-no-cycle = No day/night cycle is active on this map.
command-cfgetdayinfo-success = Day {$day}, {$phase}, {$time}

dayphase-night = Night
dayphase-early-morning = Early morning
dayphase-day = Day
dayphase-evening = Evening
dayphase-late-evening = Late evening
dayphase-deep-night = Deep night
dayphase-dawn = Dawn
dayphase-morning = Morning
dayphase-afternoon = Afternoon

command-cfgettemperature-description = Show the current map temperature.
command-cfgettemperature-help = Usage: cfgettemperature <mapId>
command-cfgettemperature-usage = Usage: cfgettemperature <mapId>
command-cfgettemperature-invalid-map = Invalid map id.
command-cfgettemperature-no-controller = No temperature controller on this map.
command-cfgettemperature-success = Temperature: {$kelvin} K ({$celsius} C), zone {$zone}
