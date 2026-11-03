using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Generation;

public abstract class SharedRMCReagentAnalyzerSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    private bool _inserting;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, InteractUsingEvent>(OnInteractUsing, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, InteractHandEvent>(OnInteractHand, before: [typeof(ItemSlotsSystem)]);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, RMCReagentAnalyzerConfigureDoAfterEvent>(OnConfigureDoAfter);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
    }

    private void OnInteractUsing(Entity<RMCReagentAnalyzerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = args.User;
        if (ent.Comp.Processing)
        {
            _popup.PopupClient($"{Name(ent)} is still processing!", ent, user, PopupType.SmallCaution);
            return;
        }

        if (TryComp(ent, out RequiresSkillComponent? requiresSkill) &&
            !_skills.HasAllSkills(user, requiresSkill.Skills))
        {
            _popup.PopupClient("You have no idea how to use this.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!HasComp<VialComponent>(args.Used))
        {
            _popup.PopupClient($"{Name(ent)} only accepts samples in vials.", ent, user, PopupType.SmallCaution);
            return;
        }

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SampleSlotId, out var slot))
            return;

        if (slot.HasItem)
        {
            _popup.PopupClient($"Something is already loaded into {Name(ent)}.", ent, user, PopupType.SmallCaution);
            return;
        }

        _inserting = true;
        var inserted = _itemSlots.TryInsertFromHand(ent, slot, user, excludeUserAudio: true);
        _inserting = false;
        if (!inserted)
            return;

        _popup.PopupClient($"You insert {Name(args.Used)} and start configuring {Name(ent)}.", ent, user);

        var doAfter = new DoAfterArgs(EntityManager, user, ent.Comp.ConfigureDelay, new RMCReagentAnalyzerConfigureDoAfterEvent(), ent, ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };
        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnInteractHand(Entity<RMCReagentAnalyzerComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        var user = args.User;
        if (ent.Comp.Processing)
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

    private void OnConfigureDoAfter(Entity<RMCReagentAnalyzerComponent> ent, ref RMCReagentAnalyzerConfigureDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (ent.Comp.Processing)
            return;

        if (GetSample(ent) is not { } sample)
        {
            _popup.PopupClient("Someone else removed the sample. Make up your mind!", ent, args.User, PopupType.SmallCaution);
            return;
        }

        ent.Comp.Processing = true;
        ent.Comp.ProcessEndTime = Timing.CurTime + ent.Comp.ProcessDelay;
        ent.Comp.ProcessSoundTime = ent.Comp.ProcessEndTime - ent.Comp.ProcessSoundLead;
        ent.Comp.VisualState = IsValidSample(ent, sample)
            ? RMCReagentAnalyzerVisualState.Processing
            : RMCReagentAnalyzerVisualState.Error;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void OnInsertAttempt(Entity<RMCReagentAnalyzerComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (ent.Comp.Processing || !_inserting)
            args.Cancelled = true;
    }

    private void OnEjectAttempt(Entity<RMCReagentAnalyzerComponent> ent, ref ItemSlotEjectAttemptEvent args)
    {
        if (ent.Comp.Processing)
            args.Cancelled = true;
    }

    private void OnInserted(Entity<RMCReagentAnalyzerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlotId || ent.Comp.Processing)
            return;

        ent.Comp.VisualState = RMCReagentAnalyzerVisualState.Sample;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    private void OnRemoved(Entity<RMCReagentAnalyzerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != ent.Comp.SampleSlotId || ent.Comp.Processing)
            return;

        ent.Comp.VisualState = RMCReagentAnalyzerVisualState.Idle;
        Dirty(ent);
        UpdateAppearance(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCReagentAnalyzerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.ProcessSoundTime is { } soundTime && Timing.CurTime >= soundTime)
            {
                comp.ProcessSoundTime = null;
                if (_net.IsServer)
                    _audio.PlayPvs(comp.ProcessSound, uid);
            }

            if (!comp.Processing || comp.ProcessEndTime is not { } end || Timing.CurTime < end)
                continue;

            comp.ProcessEndTime = null;
            DoAnalysis((uid, comp));
        }
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

    protected void UpdateAppearance(Entity<RMCReagentAnalyzerComponent> ent)
    {
        _appearance.SetData(ent, RMCReagentAnalyzerVisuals.State, ent.Comp.VisualState);
    }

    protected virtual void DoAnalysis(Entity<RMCReagentAnalyzerComponent> ent)
    {
    }
}
