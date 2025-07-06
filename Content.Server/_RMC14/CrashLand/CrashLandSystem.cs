using Content.Shared._RMC14.CrashLand;
using Content.Shared.Damage;
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
            Dirty(uid, crashLanding);
            if (!(crashLanding.RemainingTime <= 0))
                continue;

            if (crashLanding.DoDamage)
            {
                var damage = new DamageSpecifier
                {
                    DamageDict =
                    {
                        [CrashLandDamageType] = CrashLandDamageAmount,
                    },
                };

                Damageable.TryChangeDamage(uid, damage);
            }

            _audio.PlayPvs(crashLandable.CrashSound, uid);
            RemComp<CrashLandingComponent>(uid);
            Blocker.UpdateCanMove(uid);

        }
    }
}
