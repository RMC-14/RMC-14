using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Standing;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.SwiftSteps;

public sealed class XenoSwiftStepsSystem : EntitySystem
{
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly XenoRestSystem _xenoRest = default!;
    [Dependency] private readonly StandingStateSystem _standingState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    //[Dependency] private readonly INetManager _net = default!;
    //[Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoSwiftStepsComponent, RMCBeforeProjectileAccuracyEvent>(OnSwiftStepsBeforeAccuracy);
    }

    private void OnSwiftStepsBeforeAccuracy(Entity<XenoSwiftStepsComponent> xeno, ref RMCBeforeProjectileAccuracyEvent args)
    {
        if (_xenoRest.IsResting(xeno.Owner) || _standingState.IsDown(xeno) ||
            !_mob.IsAlive(xeno)) // Lying down or unconscious
        {
            ResetCount(xeno);
            return;
        }

        if (HasComp<IgnoreSwiftStepsComponent>(args.Projectile))
            return;

        var time = _timing.CurTime;

        CheckIgnoredBullets(xeno, time);

        if (xeno.Comp.CountingExpireAt != null && time >= xeno.Comp.CountingExpireAt)
            xeno.Comp.ProjectilesCounted = 0;

        xeno.Comp.CountingExpireAt = time + xeno.Comp.CountingDuration;

        var ev = new RMCGetSwiftStepsThresholdEvent(xeno.Comp.BaseDodgeThreshold);

        RaiseLocalEvent(xeno, ref ev);

        var projectileNetEnt = GetNetEntity(args.Projectile);

        if (!xeno.Comp.IgnoreBullets.ContainsKey(projectileNetEnt))
        {
            xeno.Comp.ProjectilesCounted++;
            xeno.Comp.IgnoreBullets.Add(projectileNetEnt, time + xeno.Comp.IgnoreDuration);
        }


        if (xeno.Comp.ProjectilesCounted >= ev.Threshold)
        {
            ResetCount(xeno);

            _jitter.DoJitter(xeno, xeno.Comp.JitterTime, true);

            args.GuaranteedMiss = true;

            /* Too Repetitive
            var selfMsg = Loc.GetString("rmc-xeno-swift-steps-self", ("bullet", args.Projectile));
            _popup.PopupEntity(selfMsg, xeno, xeno, PopupType.SmallCaution);

            foreach (var session in Filter.PvsExcept(xeno, entityManager: EntityManager).Recipients)
            {
                if (session.AttachedEntity is not { } viewer)
                    continue;

                var name = Identity.Name(xeno, EntityManager, viewer);
                var othersMsg = Loc.GetString("rmc-xeno-swift-steps-others", ("user", name), ("bullet", args.Projectile));
                _popup.PopupEntity(othersMsg, xeno, session, PopupType.SmallCaution);
            }
            */

            return;
        }

        Dirty(xeno);
    }

    private void ResetCount(Entity<XenoSwiftStepsComponent> xeno)
    {
        xeno.Comp.ProjectilesCounted = 0;
        xeno.Comp.CountingExpireAt = null;
        Dirty(xeno);
    }

    private void CheckIgnoredBullets(Entity<XenoSwiftStepsComponent> xeno, TimeSpan currTime)
    {
        List<NetEntity> toRemove = new();
        foreach (var bullet in xeno.Comp.IgnoreBullets)
        {
            if (bullet.Value >= currTime)
                continue;

            toRemove.Add(bullet.Key);
        }

        foreach (var removal in toRemove)
            xeno.Comp.IgnoreBullets.Remove(removal);
    }
}
