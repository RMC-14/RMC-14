using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Ghost.Roles.Components;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

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

        // Check if this is a xeno ghost role by checking if the spawned entity will have XenoComponent
        if (!TryComp<GhostRoleMobSpawnerComponent>(ent, out var spawner))
            return;

        if (string.IsNullOrEmpty(spawner.Prototype))
            return;

        if (!IoCManager.Resolve<IPrototypeManager>().TryIndex<EntityPrototype>(spawner.Prototype, out var proto))
            return;

        if (!proto.TryGetComponent<XenoComponent>(out _, IoCManager.Resolve<IComponentFactory>()))
            return;

        // Check if player is banished
        if (!_banish.CanTakeXenoRole(args.Player.UserId))
        {
            var delay = _banish.GetDelayedLarvaTime(args.Player.UserId);
            var reason = delay.HasValue 
                ? Loc.GetString("rmc-banish-cant-take-role-time", ("seconds", (int)delay.Value.TotalSeconds))
                : Loc.GetString("rmc-banish-cant-take-role");
            args = args with { Cancelled = true, Reason = reason, Target = args.Player.AttachedEntity };
        }
    }
}

[ByRefEvent]
public record struct GhostRoleRequirementsCheckEvent(ICommonSession Player, bool Cancelled = false, string? Reason = null, EntityUid? Target = null);