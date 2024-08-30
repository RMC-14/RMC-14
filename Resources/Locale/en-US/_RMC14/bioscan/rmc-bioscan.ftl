rmc-bioscan-ares-announcement = [color=white][font size=16][bold]ARES v3.2 Bioscan Status[/bold][/font][/color][color=red][font size=14][bold]
    {$message}[/bold][/font][/color]

rmc-bioscan-ares = Bioscan complete.

  Sensors indicate { $shipUncontained ->
    [0] no
    *[other] {$shipUncontained}
  } unknown lifeform { $shipUncontained ->
    [0] signatures
    [1] signature
    *[other] signatures
  } present on the ship{ $shipLocation ->
    [none] {""}
    *[other], including one in {$shipLocation},
  } and { $onPlanet ->
    [0] no
    *[other] approximately {$onPlanet}
  } { $onPlanet ->
    [0] signatures
    [1] signature
    *[other] signatures
  } located elsewhere{ $planetLocation ->
    [none].
    *[other], including one in {$planetLocation}
  }

rmc-bioscan-xeno-announcement = [color=#318850][font size=14][bold]The Queen Mother reaches into your mind from worlds away.
  {$message}[/bold][/font][/color]

rmc-bioscan-xeno = To my children and their Queen: I sense { $onShip ->
  [0] no hosts
  [1] approximately 1 host
  *[other] approximately {$onShip} hosts
} in the metal hive{ $shipLocation ->
  [none] {""}
  *[other], including one in {$shipLocation},
} and {$onPlanet ->
  [0] none
  *[other] {$onPlanet}
} scattered elsewhere{$planetLocation ->
  [none].
  *[other], including one in {$planetLocation}
}
