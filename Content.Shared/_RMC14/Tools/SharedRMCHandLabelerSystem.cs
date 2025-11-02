using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Tools;

public abstract class SharedRMCHandLabelerSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private const string PillCanisterTag = "PillCanister";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCHandLabelerComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<RMCHandLabelerComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<RMCHandLabelerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<RMCHandLabelerComponent, AfterInteractEvent>(OnAfterInteract, before: new[] { typeof(SharedHandLabelerSystem) });
    }

    private void OnExamine(Entity<RMCHandLabelerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("rmc-hand-labeler-examine-labels",
            ("current", ent.Comp.LabelsLeft),
            ("max", ent.Comp.MaxLabels)));
        args.PushMarkup(Loc.GetString("rmc-hand-labeler-examine-refill"));
    }

    private void OnInteractUsing(Entity<RMCHandLabelerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<PaperComponent>(args.Used, out _))
            return;

        if (!_net.IsServer)
            return;

        if (ent.Comp.LabelsLeft >= ent.Comp.MaxLabels)
        {
            _popup.PopupEntity(Loc.GetString("rmc-hand-labeler-already-full"), ent, args.User);
            args.Handled = true;
            return;
        }

        ent.Comp.LabelsLeft = ent.Comp.MaxLabels;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("rmc-hand-labeler-refill"), ent, args.User);
        QueueDel(args.Used);

        args.Handled = true;
    }
    // Pill bottle interaction because storage system marks args.Handled = true even if insertion fails.
    private void OnBeforeRangedInteract(Entity<RMCHandLabelerComponent> ent, ref BeforeRangedInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!_tag.HasTag(target, PillCanisterTag))
            return;

        if (!TryComp<HandLabelerComponent>(ent, out var labeler))
            return;

        OnPillBottleInteract(ent, target, args.User);

        var labelText = labeler.AssignedLabel;

        if (!string.IsNullOrEmpty(labelText))
        {
            if (!_whitelist.IsWhitelistFail(labeler.Whitelist, target) && ent.Comp.LabelsLeft > 0)
            {
                ent.Comp.LabelsLeft--;
                Dirty(ent);
                _audio.PlayPredicted(ent.Comp.LabelSound, target, args.User);

                if (_net.IsServer)
                {
                    var labelSys = EntityManager.System<LabelSystem>();
                    labelSys.Label(target, labelText);
                }
            }
            else if (ent.Comp.LabelsLeft <= 0)
            {
                _popup.PopupEntity(Loc.GetString("rmc-hand-labeler-out-of-labels"), ent, args.User);
            }
        }
        else if (TryComp<LabelComponent>(target, out var labelComp) && !string.IsNullOrEmpty(labelComp.CurrentLabel))
        {
            _audio.PlayPredicted(ent.Comp.RemoveLabelSound, target, args.User);

            if (_net.IsServer)
            {
                var labelSys = EntityManager.System<LabelSystem>();
                labelSys.Label(target, null);
            }
        }

        args.Handled = true;
    }
    // Non pill bottle interaction
    private void OnAfterInteract(Entity<RMCHandLabelerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<HandLabelerComponent>(ent, out var labeler))
            return;

        if (string.IsNullOrEmpty(labeler.AssignedLabel))
        {
            if (TryComp<LabelComponent>(target, out var labelComp) &&
                !string.IsNullOrEmpty(labelComp.CurrentLabel))
            {
                _audio.PlayPredicted(ent.Comp.RemoveLabelSound, target, args.User);
            }
            return;
        }

        if (_whitelist.IsWhitelistFail(labeler.Whitelist, target))
            return;

        switch (ent.Comp.LabelsLeft)
        {
            case <= 0:
                _popup.PopupEntity(Loc.GetString("rmc-hand-labeler-out-of-labels"), ent, args.User);
                args.Handled = true;
                return;
            case > 0:
                ent.Comp.LabelsLeft--;
                Dirty(ent);
                break;
        }

        _audio.PlayPredicted(ent.Comp.LabelSound, target, args.User);
    }

    protected virtual void OnPillBottleInteract(EntityUid labeler, EntityUid pillBottle, EntityUid user)
    {
    }
}
