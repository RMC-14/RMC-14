rmc-overwatch-crate-status =
    { $hasCrate ->
        [true] [color=green][bold] \\[ CRATE LOADED \\][/bold][/color]
       *[false] [color=red][bold] \\[ NO CRATE LOADED \\][/bold][/color]
    }
rmc-overwatch-crate-cooldown = [color=#D3B400][bold]\\ [ COOLDOWN - ($SupplyTimeLeft) SECONDS \\][/bold][/color]
rmc-overwatch-ob-cooldown = [color=#D3B400][bold]\\ [ COOLDOWN - ($OrbitalTimeLeft) SECONDS \\][/bold][/color]
rmc-overwatch-ready-status =
    { $isReady ->
        [true] [color=green][bold] \\[ READY \\][/bold][/color]
       *[false] [color=red][bold] \\[ NOT READY \\][/bold][/color]
    }
