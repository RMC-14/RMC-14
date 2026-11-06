namespace Content.Shared._RMC14.OrbitalCannon;

public readonly record struct OrbitalCannonStatusSummary(
    OrbitalCannonStatus Status,
    string? Warhead,
    int Fuel,
    int? RequiredFuel,
    TimeSpan NextFire);
