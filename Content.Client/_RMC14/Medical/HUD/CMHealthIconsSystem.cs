using Content.Shared._RMC14.Connection;
using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Medical.HUD;

public sealed class CMHealthIconsSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly RMCUnrevivableSystem _unrevivable = default!;

    private static readonly ProtoId<HealthIconPrototype> BaseDeadIcon = "CMHealthIconDead";

    public StatusIconData GetDeadIcon()
    {
        return _prototype.Index<HealthIconPrototype>(BaseDeadIcon);
    }

    public IReadOnlyList<StatusIconData> GetIcons(Entity<DamageableComponent> damageable)
    {
        var icons = new List<StatusIconData>();
        var icon = RMCHealthIconTypes.Healthy;

        if (!TryComp<RMCHealthIconsComponent>(damageable, out var iconsComp))
            return icons;

        if (_mobState.IsDead(damageable))
        {
            var stage = _unrevivable.GetUnrevivableStage(damageable.Owner, 4);
            if (_unrevivable.IsUnrevivable(damageable))
                icon = RMCHealthIconTypes.Dead;
            else if (TryComp<MindCheckComponent>(damageable, out var mind) && !mind.ActiveMindOrGhost)
                icon = RMCHealthIconTypes.DeadDNR;
            else if (stage <= 1)
                icon = RMCHealthIconTypes.DeadDefib;
            else if (stage == 2)
                icon = RMCHealthIconTypes.DeadClose;
            else if (stage == 3)
                icon = RMCHealthIconTypes.DeadAlmost;
        }

        if (iconsComp.Icons.TryGetValue(icon, out var iconToUse))
            icons.Add(_prototype.Index(iconToUse));

        return icons;
    }
}
