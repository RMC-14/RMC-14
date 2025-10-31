using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Tools;

public abstract class SharedRMCHandLabelerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCHandLabelerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RMCHandLabelerComponent, ItemToggledEvent>(OnItemToggled);
        SubscribeLocalEvent<RMCHandLabelerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCHandLabelerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnExamine(Entity<RMCHandLabelerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-hand-labeler-examine-labels",
            ("current", ent.Comp.LabelsLeft),
            ("max", ent.Comp.MaxLabels)));
        args.PushMarkup(Loc.GetString("rmc-hand-labeler-examine-refill"));
    }

    private void OnItemToggled(Entity<RMCHandLabelerComponent> ent, ref ItemToggledEvent args)
    {
        ent.Comp.Mode = args.Activated;
        Dirty(ent);

        var message = args.Activated
            ? Loc.GetString("rmc-hand-labeler-turned-on")
            : Loc.GetString("rmc-hand-labeler-turned-off");

        if (args.User != null)
            _popup.PopupEntity(message, args.User.Value, args.User.Value);
    }

    private void OnAfterInteract(Entity<RMCHandLabelerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        if (!TryComp<HandLabelerComponent>(ent, out var labeler))
            return;

        var target = args.Target.Value;

        if (!ent.Comp.Mode) // If toggled off, can only remove labels
        {
            if (!TryComp<LabelComponent>(target, out var labelComp) || string.IsNullOrEmpty(labelComp.CurrentLabel))
            {
                _popup.PopupEntity(Loc.GetString("rmc-hand-labeler-no-label"), ent, args.User);
                args.Handled = true;
                return;
            }

            if (ent.Comp.RemoveLabelSound != null)
                _audio.PlayPredicted(ent.Comp.RemoveLabelSound, target, args.User);
            // Let base system handle the actual removal
            return;
        }

        if (ent.Comp.LabelsLeft <= 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hand-labeler-out-of-labels"), ent, args.User);
            args.Handled = true;
            return;
        }

        if (string.IsNullOrEmpty(labeler.AssignedLabel))
        {
            _popup.PopupEntity(Loc.GetString("rmc-hand-labeler-no-text-set"), ent, args.User);
            args.Handled = true;
            return;
        }

        if (ent.Comp.LabelsLeft > 0)
        {
            ent.Comp.LabelsLeft--;
            Dirty(ent);
        }

        if (ent.Comp.LabelSound != null)
            _audio.PlayPredicted(ent.Comp.LabelSound, target, args.User);
        // Let base HandLabeler system handle the actual labeling
    }

    private void OnInteractUsing(Entity<RMCHandLabelerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PaperComponent>(args.Used, out _))
            return;

        if (ent.Comp.LabelsLeft >= ent.Comp.MaxLabels)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hand-labeler-already-full"), ent, args.User);
            args.Handled = true;
            return;
        }

        ent.Comp.LabelsLeft = ent.Comp.MaxLabels;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("rmc-hand-labeler-refilled"), ent, args.User);

        if (_net.IsServer)
            QueueDel(args.Used);

        args.Handled = true;
    }
}
