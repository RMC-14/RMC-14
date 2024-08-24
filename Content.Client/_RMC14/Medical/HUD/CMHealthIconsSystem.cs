using Content.Shared._RMC14.Xenonids;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Medical.HUD;

public sealed class CMHealthIconsSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly ProtoId<HealthIconPrototype> Healthy = "CMHealthIconHealthy";
    private static readonly ProtoId<HealthIconPrototype> DeadDefib = "CMHealthIconDeadDefib";
    private static readonly ProtoId<HealthIconPrototype> DeadClose = "CMHealthIconDeadClose";
    private static readonly ProtoId<HealthIconPrototype> DeadAlmost = "CMHealthIconDeadClose";
    private static readonly ProtoId<HealthIconPrototype> DeadDNR = "CMHealthIconDeadDNR";
    private static readonly ProtoId<HealthIconPrototype> Dead = "CMHealthIconDead";
    private static readonly ProtoId<HealthIconPrototype> HCDead = "CMHealthIconDead";

    public StatusIconData GetDeadIcon()
    {
        return _prototype.Index<HealthIconPrototype>(Dead);
    }

    public IReadOnlyList<StatusIconData> GetIcons(Entity<DamageableComponent> damageable)
    {
        var icons = new List<StatusIconData>();

        if (HasComp<XenoComponent>(damageable))
            return icons;

        if (_mobState.IsAlive(damageable) ||
            _mobState.IsCritical(damageable) ||
            !_mobState.IsDead(damageable))
        {
            icons.Add(_prototype.Index(Healthy));
            return icons;
        }

        if (_mobState.IsDead(damageable))
        {
            if (CompOrNull<SSDIndicatorComponent>(damageable)?.IsSSD ?? false)
            {
                icons.Add(_prototype.Index(DeadDNR));
                return icons;
            }
        }

        // TODO RMC14 don't use perishable
        if (!TryComp(damageable, out PerishableComponent? perishable) ||
            perishable.Stage <= 1)
        {
            icons.Add(_prototype.Index(DeadDefib));
            return icons;
        }
        else if (perishable.Stage == 2)
        {
            icons.Add(_prototype.Index(DeadClose));
            return icons;
        }
        else if (perishable.Stage == 3)
        {
            icons.Add(_prototype.Index(DeadAlmost));
            return icons;
        }

        icons.Add(GetDeadIcon());
        return icons;
    }
}
