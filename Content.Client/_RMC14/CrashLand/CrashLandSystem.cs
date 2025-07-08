using Content.Client._RMC14.ParaDrop;
using Content.Client._RMC14.Sprite;
using Content.Shared._RMC14.CrashLand;
using Content.Shared.ParaDrop;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.CrashLand;

public sealed class CrashLandSystem : SharedCrashLandSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animPlayer = default!;
    [Dependency] private readonly ParaDropSystem _paraDrop = default!;
    [Dependency] private readonly RMCSpriteSystem _rmcSprite = default!;

    private const string CrashingAnimationKey = "crashing-animation";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<CrashLandableComponent, CrashLandingComponent>();
        while (query.MoveNext(out var uid, out var crashLandable, out var crashLanding))
        {
            if (!HasComp<SkyFallingComponent>(uid))
            {
                if (!_animPlayer.HasRunningAnimation(uid, CrashingAnimationKey) && crashLandable.LastCrash != null)
                    _paraDrop.PlayFallAnimation(uid, crashLandable.CrashDuration, crashLanding.RemainingTime, crashLandable.FallHeight, CrashingAnimationKey);

                _rmcSprite.UpdatePosition(uid);
            }
        }
    }
}
