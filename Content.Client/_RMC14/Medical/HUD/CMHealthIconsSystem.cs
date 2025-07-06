using Content.Shared._RMC14.Connection;
using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared._RMC14.Medical.HUD.Components;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Medical.HUD;

public sealed class CMHealthIconsSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const string Healthy = "Healthy";
    private const string DeadDefib = "DeadDefib";
    private const string DeadClose = "DeadClose";
    private const string DeadAlmost = "DeadAlmost";
    private const string DeadDNR = "DeadDNR";
    private const string Dead = "Dead";
    private const string HCDead = "HCDead";

    private static readonly ProtoId<HealthIconPrototype> BaseDeadIcon = "CMHealthIconDead";

    public StatusIconData GetDeadIcon()
    {
        return _prototype.Index<HealthIconPrototype>(BaseDeadIcon);
    }

    public IReadOnlyList<StatusIconData> GetIcons(Entity<DamageableComponent> damageable)
    {
        var icons = new List<StatusIconData>();
        var icon = Healthy;

        if (!TryComp<RMCHealthIconsComponent>(damageable, out var iconsComp))
            return icons;

        // TODO RMC14 don't use perishable
        if (HasComp<CMDefibrillatorBlockedComponent>(damageable)
            || TryComp(damageable, out PerishableComponent? perishable)
            && perishable.Stage >= 4)
        {
            icon = Dead;
            if (iconsComp.Icons.TryGetValue(icon, out var deadIcon))
                icons.Add(_prototype.Index(deadIcon));

            return icons;
        }

        if (perishable == null || perishable.Stage <= 1)
            icon = DeadDefib;
        else if (perishable.Stage == 2)
            icon = DeadClose;
        else if (perishable.Stage == 3)
            icon = DeadAlmost;

        if (_mobState.IsDead(damageable))
        {
            if (TryComp<MindCheckComponent>(damageable, out var mind) && !mind.ActiveMindOrGhost)
                icon = DeadDNR;
        }

        if (iconsComp.Icons.TryGetValue(icon, out var iconToUse))
            icons.Add(_prototype.Index(iconToUse));

        return icons;
    }
}
