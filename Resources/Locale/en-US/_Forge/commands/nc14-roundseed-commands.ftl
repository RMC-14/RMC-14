nc14-command-setroundseed-description = Queue a seed for the next round (lobby only).
nc14-command-setroundseed-help = Usage: setroundseed <seed>
nc14-command-setroundseed-usage = Usage: setroundseed <seed>
nc14-command-setroundseed-not-in-lobby = Command input is only available in the lobby.
nc14-command-setroundseed-success = Seed "{$seed}" will be used for the next round.
nc14-command-setroundseed-server-console = Server console

nc14-command-getroundseed-description = Show the current round seed.
nc14-command-getroundseed-help = Usage: getroundseed
nc14-command-getroundseed-no-seed = Seed is not available yet.
nc14-command-getroundseed-success = Current round seed: {$seed}

nc14-round-seed-system-actor-server = Server
nc14-round-seed-system-log-queued-next = {$by} queued seed "{$seedText}" (value {$seedValue}) for the next round
nc14-round-seed-system-log-seed-queued = Round {$roundId} seed queued: "{$seedText}" -> {$seedValue}
nc14-round-seed-system-log-seed-generated = Round {$roundId} seed generated: {$seedValue}

nc14-command-getdayphase-description = Show the current day phase for a map.
nc14-command-getdayphase-help = Usage: nc_getdayphase <mapId>
nc14-command-getdayphase-usage = Usage: nc_getdayphase <mapId>
nc14-command-getdayphase-invalid-map = Invalid map id.
nc14-command-getdayphase-no-cycle = No day/night cycle is active on this map.
nc14-command-getdayphase-success = Current day phase: {$phase} (day {$day})

nc14-command-getdaytime-description = Show the current in-world time (HH:MM) for a map.
nc14-command-getdaytime-help = Usage: nc_getdaytime <mapId>
nc14-command-getdaytime-usage = Usage: nc_getdaytime <mapId>
nc14-command-getdaytime-invalid-map = Invalid map id.
nc14-command-getdaytime-no-cycle = No day/night cycle is active on this map.
nc14-command-getdaytime-success = Current in-world time: {$time} (day {$day})

nc14-command-getdayinfo-description = Show current day, phase and time for a map.
nc14-command-getdayinfo-help = Usage: nc_dayinfo <mapId>
nc14-command-getdayinfo-usage = Usage: nc_dayinfo <mapId>
nc14-command-getdayinfo-invalid-map = Invalid map id.
nc14-command-getdayinfo-no-cycle = No day/night cycle is active on this map.
nc14-command-getdayinfo-success = Day {$day}, {$phase}, {$time}

nc14-dayphase-night = Night
nc14-dayphase-early-morning = Early morning
nc14-dayphase-day = Day
nc14-dayphase-evening = Evening
nc14-dayphase-late-evening = Late evening
nc14-dayphase-deep-night = Deep night
nc14-dayphase-dawn = Dawn
nc14-dayphase-morning = Morning
nc14-dayphase-afternoon = Afternoon
