using System.Numerics;

namespace Content.Shared._RMC14.Medical.CrewMonitoring;

public abstract class SharedRMCPortableCrewMonitorSystem : EntitySystem
{
    public IReadOnlyList<RMCPortableCrewMonitorEntry> GetSignals(Entity<RMCPortableCrewMonitorComponent?> monitor)
    {
        if (!Resolve(monitor, ref monitor.Comp, false))
            return Array.Empty<RMCPortableCrewMonitorEntry>();

        return monitor.Comp.Signals;
    }

    public Vector2? GetOffset(Entity<RMCPortableCrewMonitorTrackingComponent?> monitor)
    {
        if (!Resolve(monitor, ref monitor.Comp, false))
            return null;

        return monitor.Comp.Offset;
    }

    public bool IsDirectionOnly(Entity<RMCPortableCrewMonitorTrackingComponent?> monitor)
    {
        return Resolve(monitor, ref monitor.Comp, false) && monitor.Comp.DirectionOnly;
    }
}
