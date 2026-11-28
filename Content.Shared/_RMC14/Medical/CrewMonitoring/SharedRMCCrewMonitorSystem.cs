namespace Content.Shared._RMC14.Medical.CrewMonitoring;

public abstract class SharedRMCCrewMonitorSystem : EntitySystem
{
    public IReadOnlyList<RMCCrewMonitorEntry> GetEntries(Entity<RMCCrewMonitorComponent?> monitor)
    {
        if (!Resolve(monitor, ref monitor.Comp, false))
            return Array.Empty<RMCCrewMonitorEntry>();

        return monitor.Comp.Entries;
    }
}
