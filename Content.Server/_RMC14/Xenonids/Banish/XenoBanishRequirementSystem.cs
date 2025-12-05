using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Xenonids.Banish;

[UsedImplicitly]
public sealed class XenoBanishRequirementSystem : EntitySystem
{
    [Dependency] private readonly XenoBanishServerSystem _banish = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostRoleComponent, GhostRoleRequirementsCheckEvent>(OnRequirementsCheck);
    }

    private void OnRequirementsCheck(Entity<GhostRoleComponent> ent, ref GhostRoleRequirementsCheckEvent args)
    {
        if (args.Cancelled)
            return;

        // Check if this is a xeno ghost role
        if (!ent.Comp.RoleName.Contains("Larva", StringComparison.OrdinalIgnoreCase) &&
            !ent.Comp.RoleName.Contains("Facehugger", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Check if player is banished
        if (!_banish.CanTakeXenoRole(args.Player.UserId))
        {
            args = args with { Cancelled = true, Reason = "You are currently unable to take xeno roles." };
        }
    }
}

[ByRefEvent]
public record struct GhostRoleRequirementsCheckEvent(ICommonSession Player, bool Cancelled = false, string? Reason = null);