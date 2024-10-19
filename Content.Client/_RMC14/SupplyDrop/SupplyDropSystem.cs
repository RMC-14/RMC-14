using Content.Client._RMC14.Overwatch;
using Content.Shared._RMC14.SupplyDrop;

namespace Content.Client._RMC14.SupplyDrop;

public sealed class SupplyDropSystem : SharedSupplyDropSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupplyDropComputerComponent, AfterAutoHandleStateEvent>(OnSupplyDropComputerState);
    }

    private void OnSupplyDropComputerState(Entity<SupplyDropComputerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is SupplyDropComputerBui supplyDropUi)
                    supplyDropUi.Refresh();

                if (bui is OverwatchConsoleBui overwatchUi)
                    overwatchUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(SupplyDropComputerBui)}\n{e}");
        }
    }
}
