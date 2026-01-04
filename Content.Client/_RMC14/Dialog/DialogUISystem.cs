using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.UserInterface;

namespace Content.Client._RMC14.Dialog;

public sealed class DialogUISystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DialogComponent, AfterAutoHandleStateEvent>(OnDialogState);
    }

    private void OnDialogState(Entity<DialogComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.TryBui<DialogBui>(ent.Owner, static bui => bui.Refresh());
    }
}
