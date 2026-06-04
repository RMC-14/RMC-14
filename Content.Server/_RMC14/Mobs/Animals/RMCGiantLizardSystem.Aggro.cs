using System.Linq;
using Content.Shared._RMC14.Mobs.Animals;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void AlertPack(EntityUid lizard, EntityUid target, RMCGiantLizardComponent comp)
    {
        var coords = Transform.GetMapCoordinates(lizard);
        foreach (var ally in Lookup.GetEntitiesInRange<RMCGiantLizardComponent>(coords, comp.PackAlertRange))
        {
            if (ally.Owner == lizard || !MobState.IsAlive(ally.Owner))
                continue;

            TryAggro(ally.Owner, target, ally.Comp);
        }
    }

    private void TryAggro(EntityUid uid, EntityUid target, RMCGiantLizardComponent comp)
    {
        if (!ValidLizardTarget(target) || Faction.IsEntityFriendly(uid, target))
            return;

        comp.LastAggroAt = Timing.CurTime;
        Faction.AggroEntity(uid, target);
    }

    private void DecayAggression(Entity<RMCGiantLizardComponent> ent)
    {
        if (WasRecentLizardTime(ent.Comp.LastAggroAt, ent.Comp.AggressionMemory))
            return;

        var hadHostiles = ClearAggression(ent);
        if (!hadHostiles || ent.Comp.NextCalmEmoteAt > Timing.CurTime)
            return;

        ent.Comp.NextCalmEmoteAt = Timing.CurTime + ent.Comp.CalmEmoteCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-calms", ("lizard", ent.Owner)), ent.Owner);
    }

    private bool ClearAggression(Entity<RMCGiantLizardComponent> ent)
    {
        var hostiles = Faction.GetHostiles(ent.Owner).ToArray();
        foreach (var hostile in hostiles)
        {
            Faction.DeAggroEntity(ent.Owner, hostile);
        }

        ent.Comp.LastAggroAt = TimeSpan.MinValue;
        return hostiles.Length > 0;
    }
}
