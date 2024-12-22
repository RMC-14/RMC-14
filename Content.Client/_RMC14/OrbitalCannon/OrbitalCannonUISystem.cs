using Content.Shared._RMC14.OrbitalCannon;

namespace Content.Client._RMC14.OrbitalCannon;

public sealed class OrbitalCannonUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<OrbitalCannonComputerComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnState(Entity<OrbitalCannonComputerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is OrbitalCannonComputerBui cannonBui)
                    cannonBui.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(OrbitalCannonComputerBui)}\n{e}");
        }
    }
}
