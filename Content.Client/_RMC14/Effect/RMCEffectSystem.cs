using Content.Shared._RMC14.Effect;
using Content.Shared._RMC14.Stealth;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client._RMC14.Effect;

public sealed class RMCEffectSystem : SharedRMCEffectSystem
{
    // Most effects are pretty large and flashy so we're dividing the opacity of the parent by 3 before applying it to the effect.
    private const int OpacityDivider = 3;

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

        var query2 = EntityQueryEnumerator<RMCEffectComponent>();
        while (query2.MoveNext(out var uid, out var effect))
        {
            var parent = Transform(uid).ParentUid;

            if (!TryComp(parent, out SpriteComponent? parentSprite))
                return;

            if (!TryComp(uid, out SpriteComponent? sprite))
                return;

            // Only apply the reduced opacity to the effect if the parent's opacity is < 1.
            if (TryComp(parent, out EntityActiveInvisibleComponent? invisible) && invisible.Opacity < 1)
                sprite.Color = sprite.Color.WithAlpha(invisible.Opacity / OpacityDivider);
            else if (sprite.Color.A < 1)
                sprite.Color = sprite.Color.WithAlpha(parentSprite.Color.A / OpacityDivider);
        }
    }
}
