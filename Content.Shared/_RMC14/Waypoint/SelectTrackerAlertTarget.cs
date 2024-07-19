using Content.Shared.Alert;
using JetBrains.Annotations;

namespace Content.Shared._RMC14.Waypoint;

[UsedImplicitly]
[DataDefinition]
public sealed partial class SelectTrackerAlertTarget : IAlertClick
{
    public void AlertClicked(EntityUid player)
    {
        var entityManager = IoCManager.Resolve<IEntityManager>();
        var trackerSystem = entityManager.System<TrackerAlertSystem>();
        trackerSystem.OpenSelectUI(player);
    }
}
