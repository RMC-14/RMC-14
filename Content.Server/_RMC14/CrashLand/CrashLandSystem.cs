using Content.Shared._RMC14.CrashLand;
using Robust.Server.Audio;

namespace Content.Server._RMC14.CrashLand;

public sealed class CrashLandSystem : SharedCrashLandSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var crashLandingQuery = EntityQueryEnumerator<CrashLandableComponent, CrashLandingComponent>();

        while (crashLandingQuery.MoveNext(out var uid, out var crashLandable, out var crashLanding))
        {
            crashLanding.RemainingTime -= frameTime;
            if (crashLanding.RemainingTime < 0)
            {
                _audio.PlayPvs(crashLandable.CrashSound, uid);
                RemComp<CrashLandingComponent>(uid);
            }
        }
    }
}
