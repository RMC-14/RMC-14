using Content.Server.Ghost.Roles.Components;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Ghost.Roles.Components;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Xenonids.Banish;

[UsedImplicitly]
public sealed class XenoBanishRequirementSystem : EntitySystem
{
    [Dependency] private readonly XenoBanishServerSystem _banish = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IComponentFactory _compFactory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GhostRoleComponent, GhostRoleRequirementsCheckEvent>(OnRequirementsCheck);
    }

    private void OnRequirementsCheck(Entity<GhostRoleComponent> ent, ref GhostRoleRequirementsCheckEvent args)
    {
        if (args.Cancelled)
            return;

        if (!TryComp<GhostRoleMobSpawnerComponent>(ent, out var spawner))
            return;

        if (string.IsNullOrEmpty(spawner.Prototype))
            return;

        if (!_prototype.TryIndex<EntityPrototype>(spawner.Prototype, out var proto))
            return;

        if (!proto.TryGetComponent<XenoComponent>(out _, _compFactory))
            return;

        if (!_banish.CanTakeXenoRole(args.Player.UserId))
        {
            var delay = _banish.GetDelayedLarvaTime(args.Player.UserId);
            var reason = delay.HasValue
                ? Loc.GetString("rmc-banish-cant-take-role-time", ("seconds", (int)delay.Value.TotalSeconds))
                : Loc.GetString("rmc-banish-cant-take-role");
            args.Cancelled = true;
            args.Reason = reason;
            args.Target = args.Player.AttachedEntity;
        }
    }
}
