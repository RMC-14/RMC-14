using Content.Shared.Alert;

namespace Content.Shared._RMC14.NightVision;

[DataDefinition]
public sealed partial class ToggleNightVision : IAlertClick
{
    public void AlertClicked(EntityUid player)
    {
        var entities = IoCManager.Resolve<IEntityManager>();
        entities.System<SharedNightVisionSystem>().Toggle(player);
    }
}
