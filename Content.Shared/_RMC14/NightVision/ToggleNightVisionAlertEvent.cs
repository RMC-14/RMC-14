using Content.Shared.Alert;

namespace Content.Shared._RMC14.NightVision;

[DataDefinition]
public sealed partial class ToggleNightVisionAlertEvent : BaseAlertEvent
{
    public void AlertClicked(EntityUid player, AlertPrototype alert)
    {
        var entities = IoCManager.Resolve<IEntityManager>();
        entities.System<SharedNightVisionSystem>().Toggle(player);
    }
}
