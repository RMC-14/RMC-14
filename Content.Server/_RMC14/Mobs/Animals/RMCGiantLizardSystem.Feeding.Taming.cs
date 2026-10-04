using Content.Shared._RMC14.Mobs.Animals;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool TryTameToFeeder(EntityUid lizard, EntityUid feeder, RMCGiantLizardComponent comp)
    {
        if (!IsCalmForTaming(lizard, comp) ||
            !ValidLivingMob(feeder) ||
            !FactionQuery.TryComp(feeder, out var feederFactions))
        {
            return false;
        }

        var adoptedFaction = false;
        foreach (var faction in feederFactions.Factions)
        {
            if (comp.ExcludedTameFactions.Contains(faction) || !comp.AllowedTameFactions.Contains(faction))
                continue;

            Faction.AddFaction(lizard, faction);
            adoptedFaction = true;
        }

        if (!adoptedFaction)
            return false;

        Faction.IgnoreEntity(lizard, feeder);
        return true;
    }

    private bool IsCalmForTaming(EntityUid lizard, RMCGiantLizardComponent comp)
    {
        if (!MobState.IsAlive(lizard) ||
            IsOnFire(lizard) ||
            comp.Leaping ||
            comp.Retreating ||
            comp.RavageTarget != null)
        {
            return false;
        }

        if (WasRecentLizardTime(comp.LastAggroAt, comp.AggressionMemory))
        {
            return false;
        }

        foreach (var hostile in Faction.GetHostiles(lizard))
        {
            if (ValidLivingMob(hostile))
                return false;
        }

        return true;
    }
}
