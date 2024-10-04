using Content.Shared._RMC14.Power;

namespace Content.Client._RMC14.Power;

public sealed class RMCPowerSystem : SharedRMCPowerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RMCApcComponent, AfterAutoHandleStateEvent>(OnApcState);
    }

    private void OnApcState(Entity<RMCApcComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is RMCApcBui apcUi)
                    apcUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(RMCApcBui)}\n{e}");
        }
    }
}
