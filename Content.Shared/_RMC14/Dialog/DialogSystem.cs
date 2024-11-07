namespace Content.Shared._RMC14.Dialog;

public sealed class DialogSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<DialogComponent>(DialogUiKey.Key, subs =>
        {
            subs.Event<DialogChosenBuiMsg>(OnDialogChosen);
        });
    }

    private void OnDialogChosen(Entity<DialogComponent> ent, ref DialogChosenBuiMsg args)
    {
        _ui.CloseUi(ent.Owner, DialogUiKey.Key);

        var index = args.Index;
        if (index < 0 || index >= ent.Comp.Options.Count)
            return;

        var ev = new DialogChosenEvent(args.Actor, index);
        RaiseLocalEvent(ent, ref ev);
    }

    public void OpenDialog(EntityUid target, EntityUid actor, string title, List<string> options)
    {
        var dialog = EnsureComp<DialogComponent>(target);
        dialog.Options = options;
        Dirty(target, dialog);

        _ui.TryOpenUi(target, DialogUiKey.Key, actor);
        _ui.SetUiState(target, DialogUiKey.Key, new DialogBuiState(title, options));
    }
}
