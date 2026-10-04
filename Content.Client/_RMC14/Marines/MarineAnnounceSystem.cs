using Content.Client._RMC14.Marines.Announce;
using Content.Client._RMC14.Marines.CommandTablet;
using Content.Client._RMC14.Marines.GroundsideOperations;
using Content.Shared._RMC14.Marines.Announce;

namespace Content.Client._RMC14.Marines;

public sealed class MarineAnnounceSystem : SharedMarineAnnounceSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MarineCommunicationsComputerComponent, AfterAutoHandleStateEvent>(OnCommunicationsComputerState);
    }

    private void OnCommunicationsComputerState(Entity<MarineCommunicationsComputerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is MarineCommunicationsComputerBui computerUi)
                    computerUi.OnStateUpdate();
                else if (bui is CommandTabletBui commandTablet)
                    commandTablet.Refresh();
                else if (bui is GroundsideOperationsConsoleBui groundside)
                    groundside.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing marine communications UI\n{e}");
        }
    }
}
