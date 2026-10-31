using Content.Shared.Containers.ItemSlots;
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
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, ItemSlotInsertAttemptEvent>(OnInsertAttempt);
        SubscribeLocalEvent<RMCReagentAnalyzerComponent, ItemSlotEjectAttemptEvent>(OnEjectAttempt);
    }

    private void OnInsertAttempt(Entity<RMCReagentAnalyzerComponent> ent, ref ItemSlotInsertAttemptEvent args)
    {
        if (ent.Comp.Processing)
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

        ent.Comp.Processing = true;
        ent.Comp.ProcessEndTime = Timing.CurTime + ent.Comp.ProcessDelay;
        ent.Comp.ProcessSoundTime = ent.Comp.ProcessEndTime - ent.Comp.ProcessSoundLead;
        ent.Comp.VisualState = RMCReagentAnalyzerVisualState.Processing;
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
