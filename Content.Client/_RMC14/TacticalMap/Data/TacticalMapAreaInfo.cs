using Robust.Shared.Maths;

namespace Content.Client._RMC14.TacticalMap;

public readonly record struct TacticalMapAreaInfo(
    Vector2i Indices,
    string AreaName,
    string? AreaId,
    string? AreaLabel,
    string? TacticalLabel,
    bool HasArea,
    bool Cas,
    bool MortarFire,
    bool MortarPlacement,
    bool Lasing,
    bool Medevac,
    bool Paradropping,
    bool OrbitalBombard,
    bool SupplyDrop,
    bool Fulton,
    bool LandingZone,
    string? LinkedLz);
