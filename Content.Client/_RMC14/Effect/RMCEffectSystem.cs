using Content.Shared._RMC14.Effect;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Effect;

public sealed class RMCEffectSystem : SharedRMCEffectSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void FrameUpdate(float frameTime)
    {
        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<EffectAlphaAnimationComponent, SpriteComponent>();
        while (query.MoveNext(out var effect, out var sprite))
        {
            if (effect.SpawnedAt is not { } spawned)
                continue;

            var alpha = MathHelper.Lerp((spawned + effect.Delay).TotalSeconds, spawned.TotalSeconds, time.TotalSeconds);
            sprite.Color = sprite.Color.WithAlpha((float) alpha);
        }
    }
}
