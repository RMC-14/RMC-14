using Content.Shared._RMC14.Marines.ControlComputer;

using Content.Client._RMC14.Marines.CommandTablet;
using Content.Client._RMC14.Marines.GroundsideOperations;

namespace Content.Client._RMC14.Marines.ControlComputer;

public sealed class MarineControlComputerSystem : SharedMarineControlComputerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineControlComputerComponent, AfterAutoHandleStateEvent>(OnComputerState);
    }

    private void OnComputerState(Entity<MarineControlComputerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var open in ui.ClientOpenInterfaces.Values)
            {
                if (open is MarineControlComputerBui bui)
                    bui.Refresh();
                else if (open is CommandTabletBui commandTablet)
                    commandTablet.Refresh();
                else if (open is GroundsideOperationsConsoleBui groundside)
                    groundside.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing marine control UI:\n{e}");
        }
    }
}
