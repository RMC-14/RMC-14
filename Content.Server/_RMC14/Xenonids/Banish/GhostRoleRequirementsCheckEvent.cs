using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Banish;

[ByRefEvent]
public record struct GhostRoleRequirementsCheckEvent(ICommonSession Player, bool Cancelled = false, string? Reason = null, EntityUid? Target = null);
