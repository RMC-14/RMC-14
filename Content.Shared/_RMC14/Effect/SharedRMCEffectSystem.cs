using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Effect;

public abstract class SharedRMCEffectSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EffectAlphaAnimationComponent, MapInitEvent>(OnAlphaAnimationMapInit);
    }

    private void OnAlphaAnimationMapInit(Entity<EffectAlphaAnimationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.SpawnedAt = _timing.CurTime;
        Dirty(ent);
    }
}
