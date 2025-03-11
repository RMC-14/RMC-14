using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines.Squads;

[ByRefEvent]
public record struct GetMarineSquadNameEvent(string SquadName, string RoleName);
