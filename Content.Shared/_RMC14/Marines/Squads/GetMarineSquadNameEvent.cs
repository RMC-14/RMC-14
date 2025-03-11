using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Marines.Squads;

[ByRefEvent]
public record struct GetMarineSquadNameEvent(string SquadName, string RoleName);
