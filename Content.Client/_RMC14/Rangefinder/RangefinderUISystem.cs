using Content.Shared._RMC14.Rangefinder;

namespace Content.Client._RMC14.Rangefinder;

public sealed class RangefinderUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<RangefinderComponent, AfterAutoHandleStateEvent>(OnRangefinderState);
    }

    private void OnRangefinderState(Entity<RangefinderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var open in ui.ClientOpenInterfaces.Values)
            {
                if (open is RangefinderBui bui)
                    bui.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(RangefinderBui)}:\n{e}");
        }
    }
}
