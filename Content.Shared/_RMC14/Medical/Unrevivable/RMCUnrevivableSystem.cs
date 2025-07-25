using Content.Shared.Mobs;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.Unrevivable;

public sealed class RMCUnrevivableSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCRevivableComponent, MobStateChangedEvent>(OnMobstateChanged);
    }

    private void OnMobstateChanged(Entity<RMCRevivableComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            ent.Comp.UnrevivableAt = _timing.CurTime + ent.Comp.UnrevivableDelay;
        else
            ent.Comp.UnrevivableAt = TimeSpan.Zero;

        Dirty(ent);
    }

    public void AddRevivableTime(EntityUid uid, TimeSpan time)
    {
        if (!TryComp<RMCRevivableComponent>(uid, out var revivable))
            return;

        if (revivable.UnrevivableAt == TimeSpan.Zero)
            return;

        revivable.UnrevivableAt += time;
        Dirty(uid, revivable);
    }

    public bool IsUnrevivable(EntityUid uid)
    {
        return HasComp<UnrevivableComponent>(uid);
    }

    public void MakeUnrevivable(Entity<RMCRevivableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var unrevivable = EnsureComp<UnrevivableComponent>(ent);
        unrevivable.Analyzable = false;
        unrevivable.Cloneable = false;
        unrevivable.ReasonMessage = ent.Comp.UnrevivableReasonMessage;
        Dirty(ent);
    }

    public int GetUnrevivableStage(Entity<RMCRevivableComponent?> ent, int maxStages)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return 0;

        if (ent.Comp.UnrevivableAt == TimeSpan.Zero)
            return 0;

        var now = _timing.CurTime;
        var end = ent.Comp.UnrevivableAt;
        var start = ent.Comp.UnrevivableAt - ent.Comp.UnrevivableDelay;

        var progress = (now - start) / (end - start);
        return (int)(maxStages * progress);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var revivableQuery = EntityQueryEnumerator<RMCRevivableComponent>();
        while (revivableQuery.MoveNext(out var uid, out var revivable))
        {
            if (IsUnrevivable(uid))
                continue;

            if (revivable.UnrevivableAt == TimeSpan.Zero)
                continue;

            if (_timing.CurTime < revivable.UnrevivableAt)
                continue;

            MakeUnrevivable((uid, revivable));
        }
    }
}
