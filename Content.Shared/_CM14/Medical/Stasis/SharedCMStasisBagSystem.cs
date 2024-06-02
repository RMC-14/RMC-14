using Content.Shared._CM14.Medical.Wounds;
using Content.Shared._CM14.Xenos.Hugger;
using Robust.Shared.Containers;

namespace Content.Shared._CM14.Medical.Stasis;

public abstract class SharedCMStasisBagSystem : EntitySystem
{
    [Dependency] private readonly SharedXenoHuggerSystem _hugger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMStasisBagComponent, ContainerIsInsertingAttemptEvent>(OnStasisInsert);
        SubscribeLocalEvent<CMStasisBagComponent, ContainerIsRemovingAttemptEvent>(OnStasisRemove);

        SubscribeLocalEvent<CMInStasisComponent, BloodstreamMetabolizeAttemptEvent>(OnBloodstreamMetabolizeAttempt);
        SubscribeLocalEvent<CMInStasisComponent, MapInitEvent>(OnInStasisMapInit);
        SubscribeLocalEvent<CMInStasisComponent, ComponentRemove>(OnInStasisRemove);
        SubscribeLocalEvent<CMInStasisComponent, GetHuggedIncubationMultiplierEvent>(OnInStasisGetHuggedIncubationMultiplier);
        SubscribeLocalEvent<CMInStasisComponent, CMBleedAttemptEvent>(OnInStasisBleedAttempt);
    }

    private void OnStasisInsert(Entity<CMStasisBagComponent> ent, ref ContainerIsInsertingAttemptEvent args)
    {
        OnInsert(ent, args.EntityUid);
    }

    private void OnStasisRemove(Entity<CMStasisBagComponent> ent, ref ContainerIsRemovingAttemptEvent args)
    {
        OnRemove(ent, args.EntityUid);
    }

    private void OnBloodstreamMetabolizeAttempt(Entity<CMInStasisComponent> ent, ref BloodstreamMetabolizeAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnInStasisMapInit(Entity<CMInStasisComponent> ent, ref MapInitEvent args)
    {
        _hugger.RefreshIncubationMultipliers(ent.Owner);
    }

    private void OnInStasisRemove(Entity<CMInStasisComponent> ent, ref ComponentRemove args)
    {
        _hugger.RefreshIncubationMultipliers(ent.Owner);
    }

    private void OnInStasisGetHuggedIncubationMultiplier(Entity<CMInStasisComponent> ent, ref GetHuggedIncubationMultiplierEvent args)
    {
        if (ent.Comp.Running)
            args.Multiply(ent.Comp.IncubationMultiplier);
    }

    private void OnInStasisBleedAttempt(Entity<CMInStasisComponent> ent, ref CMBleedAttemptEvent args)
    {
        args.Cancelled = true;
    }

    protected virtual void OnInsert(Entity<CMStasisBagComponent> bag, EntityUid target)
    {
        EnsureComp<CMInStasisComponent>(target);
    }

    protected virtual void OnRemove(Entity<CMStasisBagComponent> bag, EntityUid target)
    {
        RemCompDeferred<CMInStasisComponent>(target);
    }
}
