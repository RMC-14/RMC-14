using Content.Shared._RMC14.Dialog;

namespace Content.Client._RMC14.Dialog;

public sealed class DialogUISystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DialogComponent, AfterAutoHandleStateEvent>(OnDialogState);
    }

    private void OnDialogState(Entity<DialogComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        try
        {
            if (!TryComp(ent, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is DialogBui dialogUi)
                    dialogUi.Refresh();
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {nameof(DialogBui)}\n{e}");
        }
    }
}
