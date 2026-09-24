using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Tools;

public abstract class SharedRMCHandLabelerSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly LabelSystem _labelSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private const string PillCanisterTag = "PillCanister";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCHandLabelerComponent, BeforeRangedInteractEvent>(OnBeforeRangedInteract);
        SubscribeLocalEvent<RMCHandLabelerComponent, AfterInteractEvent>(OnAfterInteract, before: new[] { typeof(SharedHandLabelerSystem) });
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
            if (_whitelist.IsWhitelistFail(labeler.Whitelist, target))
            {
                args.Handled = true;
                return;
            }

            ApplyLabel(target, labelText);
            _audio.PlayPredicted(ent.Comp.LabelSound, ent, args.User);
        }
        else if (TryComp<LabelComponent>(target, out var labelComp) && !string.IsNullOrEmpty(labelComp.CurrentLabel))
        {
            ApplyLabel(target, null);
            _audio.PlayPredicted(ent.Comp.RemoveLabelSound, ent, args.User);
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
                _audio.PlayPredicted(ent.Comp.RemoveLabelSound, ent, args.User);
            }
            return;
        }

        if (_whitelist.IsWhitelistFail(labeler.Whitelist, target))
            return;

        _audio.PlayPredicted(ent.Comp.LabelSound, ent, args.User);
    }

    protected virtual void OnPillBottleInteract(Entity<RMCHandLabelerComponent> labeler, EntityUid pillBottle, EntityUid user)
    {
    }

    private void ApplyLabel(EntityUid target, string? labelText)
    {
        if (_net.IsServer)
        {
            _labelSystem.Label(target, labelText);
        }
    }
}
