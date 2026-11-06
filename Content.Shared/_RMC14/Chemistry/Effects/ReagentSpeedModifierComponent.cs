using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects;

public abstract partial class ReagentSpeedModifierComponent : Component
{
    public abstract float Multiplier { get; set; }

    public abstract TimeSpan AppliedAt { get; set; }
}

public abstract class ReagentSpeedModifierSystem<T> : EntitySystem where T : ReagentSpeedModifierComponent
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<T, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnRefreshSpeed<T>(Entity<T> ent, ref RefreshMovementSpeedModifiersEvent args) where T : ReagentSpeedModifierComponent
    {
        args.ModifySpeed(ent.Comp.Multiplier, ent.Comp.Multiplier);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AppliedAt < time - TimeSpan.FromSeconds(4))
                RemCompDeferred<T>(uid);
        }
    }
}
