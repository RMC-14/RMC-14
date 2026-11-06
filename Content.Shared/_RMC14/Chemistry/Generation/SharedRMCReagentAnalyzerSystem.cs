using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Generation;

public abstract class SharedRMCReagentAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, InteractHandEvent>(OnInteractHand, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
    }

    private void OnInteractUsing(Entity<RMCReagentAnalyzerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;
        if (IsProcessing(ent))
        {
            args.Handled = true;
            _popup.PopupClient($"{Name(ent)} is still processing!", ent, user, PopupType.SmallCaution);
            return;
        }

        if (TryComp(ent, out RequiresSkillComponent? requiresSkill) &&
            !_skills.HasAllSkills(user, requiresSkill.Skills))
        {
            args.Handled = true;
            _popup.PopupClient("You have no idea how to use this.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!HasComp<VialComponent>(args.Used))
        {
            args.Handled = true;
            _popup.PopupClient($"{Name(ent)} only accepts samples in vials.", ent, user, PopupType.SmallCaution);
        }
    }

    private void OnInteractHand(Entity<RMCReagentAnalyzerComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = args.User;
        if (IsProcessing(ent))
        {
            _popup.PopupClient($"{Name(ent)} is still processing!", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlotId, out var slot) || slot.Item is not { } sample)
        {
            _popup.PopupClient($"{Name(ent)} is empty.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!_itemSlots.TryEjectToHands(ent, slot, user, excludeUserAudio: true))
            return;

        _popup.PopupClient($"You remove {Name(sample)} from {Name(ent)}.", ent, user);
    }

    private void OnInsertAttempt(Entity<RMCReagentAnalyzerComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (IsProcessing(ent))
        {
            args.Cancelled = true;
            return;
        }

        if (args.User is { } user &&
            TryComp(ent, out RequiresSkillComponent? requiresSkill) &&
            !_skills.HasAllSkills(user, requiresSkill.Skills))
        {
            args.Cancelled = true;
        }
    }

    private void OnEjectAttempt(Entity<RMCReagentAnalyzerComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (IsProcessing(ent))
            args.Cancelled = true;
    }

    private void OnInserted(Entity<RMCReagentAnalyzerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState || args.Container.ID != ent.Comp.SampleSlotId || IsProcessing(ent))
            return;

        var processing = EnsureComp<RMCReagentAnalyzerProcessingComponent>(ent);
        processing.EndTime = _timing.CurTime + ent.Comp.ProcessDelay;
        processing.SoundTime = processing.EndTime - ent.Comp.ProcessSoundLead;
        SetVisualState(ent, IsValidSample(ent, args.Entity)
            ? RMCReagentAnalyzerVisualState.Processing
            : RMCReagentAnalyzerVisualState.Error);
    }

    private void OnRemoved(Entity<RMCReagentAnalyzerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlotId || IsProcessing(ent))
            return;

        SetVisualState(ent, RMCReagentAnalyzerVisualState.Idle);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCReagentAnalyzerProcessingComponent, RMCReagentAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var processing, out var analyzer))
        {
            if (processing.SoundTime is { } soundTime && time >= soundTime)
            {
                processing.SoundTime = null;
                _audio.PlayPvs(analyzer.ProcessSound, uid);
            }

            if (time < processing.EndTime)
                continue;

            RemCompDeferred<RMCReagentAnalyzerProcessingComponent>(uid);
            DoAnalysis((uid, analyzer));
        }
    }

    private bool IsProcessing(EntityUid analyzer)
    {
        return HasComp<RMCReagentAnalyzerProcessingComponent>(analyzer);
    }

    private bool IsValidSample(Entity<RMCReagentAnalyzerComponent> ent, EntityUid sample)
    {
        return _solution.TryGetMixableSolution(sample, out _, out var solution) &&
               solution.Volume >= ent.Comp.RequiredVolume &&
               solution.Contents.Count <= 1;
    }

    protected EntityUid? GetSample(Entity<RMCReagentAnalyzerComponent> ent)
    {
        return _itemSlots.TryGetSlot(ent, ent.Comp.SampleSlotId, out var slot) ? slot.Item : null;
    }

    protected void SetVisualState(EntityUid analyzer, RMCReagentAnalyzerVisualState state)
    {
        _appearance.SetData(analyzer, RMCReagentAnalyzerVisuals.State, state);
    }

    protected virtual void DoAnalysis(Entity<RMCReagentAnalyzerComponent> ent)
    {
    }
}
