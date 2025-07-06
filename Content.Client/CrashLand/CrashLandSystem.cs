using Content.Client._RMC14.ParaDrop;
using Content.Client._RMC14.Sprite;
using Content.Shared._RMC14.CrashLand;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.CrashLand;

public sealed class CrashLandSystem : SharedCrashLandSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly ParaDropSystem _paraDrop = default!;
    [Dependency] private readonly RMCSpriteSystem _rmcSprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string CrashingAnimationKey = "crashing-animation";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<CrashLandableComponent, CrashLandingComponent>();
        while (query.MoveNext(out var uid, out var crashLandable, out _))
        {
            if (!_animPlayer.HasRunningAnimation(uid, CrashingAnimationKey) && crashLandable.LastCrash != null)
                _paraDrop.PlayFallAnimation(uid, crashLandable.CrashDuration, crashLandable.LastCrash.Value, crashLandable.FallHeight, CrashingAnimationKey);

            // This is so the animation's current location gets updated during the drop.
            _rmcSprite.UpdatePosition(uid);
        }
    }
}
