using Content.Shared._RMC14.Dropship.Fabricator;

namespace Content.Client._RMC14.Dropship.Fabricator;

public sealed class DropshipFabricatorUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipFabricatorComponent, AfterAutoHandleStateEvent>(OnFabricatorState);
    }

    private void OnFabricatorState(Entity<DropshipFabricatorComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var open in ui.ClientOpenInterfaces.Values)
            {
                if (open is DropshipFabricatorBui bui)
                    bui.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(DropshipFabricatorBui)}:\n{e}");
        }
    }
}
