using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool TryStartRavage(Entity<RMCGiantLizardComponent> ent, EntityUid target, bool requireDowned)
    {
        if (ent.Comp.RavageTarget != null ||
            ent.Comp.Leaping ||
            target == ent.Owner ||
            !ValidLizardTarget(target) ||
            (!ActorQuery.HasComp(ent.Owner) && Faction.IsEntityFriendly(ent.Owner, target)) ||
            !Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, Transform.GetMoverCoordinates(target), out var distance) ||
            distance > 1.75f)
        {
            return false;
        }

        if (requireDowned && !CanRavageTarget(target))
            return false;

        WakeRest(ent);
        StopRoam(ent, false);
        StopMovement(ent.Owner);
        ent.Comp.RavageTarget = target;
        ent.Comp.RavageHitsLeft = ent.Comp.RavageHitCount;
        ent.Comp.NextRavageAt = Timing.CurTime;
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-ravage", ("lizard", ent.Owner), ("target", target)), ent.Owner, PopupType.LargeCaution);
        return true;
    }

    private bool UpdateRavage(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.RavageTarget is not { } target)
            return false;

        if (!ValidLizardTarget(target) ||
            !Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, Transform.GetMoverCoordinates(target), out var distance) ||
            distance > 1.75f ||
            !CanRavageTarget(target))
        {
            ClearRavage(ent.Comp);
            return false;
        }

        if (ent.Comp.NextRavageAt > Timing.CurTime)
            return true;

        var damage = ent.Comp.RavageDamage;
        if (HasComp<XenoComponent>(target))
            damage += ent.Comp.XenoBonusDamage;

        Damageable.TryChangeDamage(target, damage, origin: ent.Owner, tool: ent.Owner);
        Stun.TryKnockdown(target, ent.Comp.RavageKnockdown, true);
        _dazed.TryDaze(target, ent.Comp.RavageDaze, true);
        _cameraShake.ShakeCamera(target, 2, ent.Comp.RavageCameraShakeStrength);
        _audio.PlayPvs(Random.Prob(0.5f) ? ent.Comp.SlashAttackSound : ent.Comp.BiteAttackSound, ent.Owner);

        ent.Comp.RavageHitsLeft--;
        if (ent.Comp.RavageHitsLeft <= 0)
        {
            if (ent.Comp.NextPounceAt > Timing.CurTime)
            {
                ent.Comp.NextPounceAt -= ent.Comp.RavageCooldownRefund;
                if (ent.Comp.NextPounceAt < Timing.CurTime)
                    ent.Comp.NextPounceAt = Timing.CurTime;
            }

            ClearRavage(ent.Comp);
            return false;
        }

        ent.Comp.NextRavageAt = Timing.CurTime + ent.Comp.RavageHitDelay;
        return true;
    }

    private bool CanRavageTarget(EntityUid target)
    {
        return _standing.IsDown(target) || MobState.IsIncapacitated(target);
    }

    private static void ClearRavage(RMCGiantLizardComponent comp)
    {
        comp.RavageTarget = null;
        comp.RavageHitsLeft = 0;
    }
}
