using Content.Shared._CM14.Xenos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.SSDIndicator;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Medical.HUD;

public sealed class CMHealthIconsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Healthy = "CMHealthIconHealthy";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string DeadDefib = "CMHealthIconDeadDefib";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string DeadClose = "CMHealthIconDeadClose";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string DeadAlmost = "CMHealthIconDeadClose";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string DeadDNR = "CMHealthIconDeadDNR";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Dead = "CMHealthIconDead";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HCDead = "CMHealthIconDead";

    public StatusIconData GetDeadIcon()
    {
        return _prototype.Index<StatusIconPrototype>(Dead);
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
            icons.Add(_prototype.Index<StatusIconPrototype>(Healthy));
            return icons;
        }

        if (_mobState.IsDead(damageable))
        {
            if (CompOrNull<SSDIndicatorComponent>(damageable)?.IsSSD ?? false)
            {
                icons.Add(_prototype.Index<StatusIconPrototype>(DeadDNR));
                return icons;
            }
        }

        // TODO CM14 don't use perishable
        if (!TryComp(damageable, out PerishableComponent? perishable) ||
            perishable.Stage <= 1)
        {
            icons.Add(_prototype.Index<StatusIconPrototype>(DeadDefib));
            return icons;
        }
        else if (perishable.Stage == 2)
        {
            icons.Add(_prototype.Index<StatusIconPrototype>(DeadClose));
            return icons;
        }
        else if (perishable.Stage == 3)
        {
            icons.Add(_prototype.Index<StatusIconPrototype>(DeadAlmost));
            return icons;
        }

        icons.Add(GetDeadIcon());
        return icons;
    }
}
