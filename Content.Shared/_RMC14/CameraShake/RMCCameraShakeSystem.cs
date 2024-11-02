using Content.Shared.Camera;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.CameraShake;

public sealed class RMCCameraShakeSystem : EntitySystem
{
    [Dependency] private readonly SharedCameraRecoilSystem _cameraRecoil = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public void ShakeCamera(EntityUid user, int shakes, int strength, TimeSpan? spacing = null)
    {
        spacing ??= TimeSpan.FromSeconds(0.1);

        var shaking = EnsureComp<RMCCameraShakingComponent>(user);
        shaking.Spacing = spacing.Value;
        shaking.Shakes = shakes;
        shaking.Strength = strength;

        Dirty(user, shaking);
    }

    public override void Update(float frameTime)
    {
        var time = _timing.CurTime;
        var shakingQuery = EntityQueryEnumerator<RMCCameraShakingComponent>();
        while (shakingQuery.MoveNext(out var uid, out var shaking))
        {
            if (shaking.Shakes <= 0)
            {
                RemCompDeferred<RMCCameraShakingComponent>(uid);
                continue;
            }

            if (time < shaking.NextShake)
                continue;

            shaking.Shakes--;
            shaking.NextShake = time + shaking.Spacing;
            var kick = _random.NextVector2Box(-shaking.Strength, shaking.Strength);
            _cameraRecoil.KickCamera(uid, kick);
        }
    }
}
