using Content.Shared._RMC14.Intel;

namespace Content.Client._RMC14.Intel;

public sealed class IntelUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ViewIntelObjectivesComponent, AfterAutoHandleStateEvent>(OnViewIntelObjectivesAfterState);
    }

    private void OnViewIntelObjectivesAfterState(Entity<ViewIntelObjectivesComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is ViewIntelObjectivesBui intelUi)
                    intelUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(ViewIntelObjectivesBui)}\n{e}");
        }
    }
}
