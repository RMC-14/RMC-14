command-setroundseed-description = Queue a seed for the next round (lobby only).
command-setroundseed-help = Usage: setroundseed <seed>
command-setroundseed-usage = Usage: setroundseed <seed>
command-setroundseed-not-in-lobby = Command input is only available in the lobby.
command-setroundseed-success = Seed "{$seed}" will be used for the next round.
command-setroundseed-server-console = Server console

command-getroundseed-description = Show the current round seed.
command-getroundseed-help = Usage: getroundseed
command-getroundseed-no-seed = Seed is not available yet.
command-getroundseed-success = Current round seed: {$seed}

round-seed-system-actor-server = Server
round-seed-system-log-queued-next = {$by} queued seed "{$seedText}" (value {$seedValue}) for the next round
round-seed-system-log-seed-queued = Round {$roundId} seed queued: "{$seedText}" -> {$seedValue}
round-seed-system-log-seed-generated = Round {$roundId} seed generated: {$seedValue}

command-getdayphase-description = Show the current day phase for a map.
command-getdayphase-help = Usage: getdayphase <mapId>
command-getdayphase-usage = Usage: getdayphase <mapId>
command-getdayphase-invalid-map = Invalid map id.
command-getdayphase-no-cycle = No day/night cycle is active on this map.
command-getdayphase-success = Current day phase: {$phase} (day {$day})

command-getdaytime-description = Show the current in-world time (HH:MM) for a map.
command-getdaytime-help = Usage: getdaytime <mapId>
command-getdaytime-usage = Usage: getdaytime <mapId>
command-getdaytime-invalid-map = Invalid map id.
command-getdaytime-no-cycle = No day/night cycle is active on this map.
command-getdaytime-success = Current in-world time: {$time} (day {$day})

command-getdayinfo-description = Show current day, phase and time for a map.
command-getdayinfo-help = Usage: dayinfo <mapId>
command-getdayinfo-usage = Usage: dayinfo <mapId>
command-getdayinfo-invalid-map = Invalid map id.
command-getdayinfo-no-cycle = No day/night cycle is active on this map.
command-getdayinfo-success = Day {$day}, {$phase}, {$time}

dayphase-night = Night
dayphase-early-morning = Early morning
dayphase-day = Day
dayphase-evening = Evening
dayphase-late-evening = Late evening
dayphase-deep-night = Deep night
dayphase-dawn = Dawn
dayphase-morning = Morning
dayphase-afternoon = Afternoon
