using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Popups;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool TryApplyPounceHit(Entity<RMCGiantLizardComponent> ent, EntityUid target)
    {
        if (target == ent.Owner || !ValidLizardTarget(target))
            return false;

        if (TryComp<RMCLeapProtectionComponent>(target, out var protection) &&
            TryBlockPounce(ent, target, protection))
        {
            return true;
        }

        if (_size.TryGetSize(target, out var targetSize) && targetSize >= RMCSizes.Big)
        {
            StopPounce(ent);
            Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-pounce-too-large", ("target", target)), target, ent.Owner, PopupType.MediumCaution);
            return true;
        }

        if (Faction.IsEntityFriendly(ent.Owner, target))
        {
            StopPounce(ent);
            return true;
        }

        Stun.TryKnockdown(target, ent.Comp.PounceKnockdown, true);
        _dazed.TryDaze(target, ent.Comp.RavageDaze, true);
        _cameraShake.ShakeCamera(target, 2, ent.Comp.RavageCameraShakeStrength);
        Damageable.TryChangeDamage(target, ent.Comp.PounceDamage, origin: ent.Owner, tool: ent.Owner);
        _audio.PlayPvs(ent.Comp.HissSound, ent.Owner);

        TryAggro(ent.Owner, target, ent.Comp);
        StopPounce(ent);
        TryStartRavage(ent, target, false);
        return true;
    }

    private bool TryApplyPounceObjectHit(Entity<RMCGiantLizardComponent> ent, EntityUid target)
    {
        if (target == ent.Owner || TerminatingOrDeleted(target) || MobQuery.HasComp(target))
            return false;

        if (TryComp<RMCLeapProtectionComponent>(target, out var protection) &&
            TryBlockPounce(ent, target, protection))
        {
            return true;
        }

        if (!DamageableQuery.HasComp(target) ||
            ItemQuery.HasComp(target) ||
            !XformQuery.TryGetComponent(target, out var xform) ||
            !xform.Anchored)
        {
            return false;
        }

        if (HasComp<DirectionalAttackBlockerComponent>(target) &&
            !_directionalBlock.IsAttackBlocked(ent.Owner, target))
        {
            return false;
        }

        StopPounce(ent);
        Damageable.TryChangeDamage(target, ent.Comp.PounceObstacleDamage, origin: ent.Owner, tool: ent.Owner);
        Stun.TryKnockdown(ent.Owner, ent.Comp.PounceObstacleKnockdown, true);
        _size.KnockBack(ent.Owner, Transform.GetMapCoordinates(target), ent.Comp.PounceBlockedKnockback, ent.Comp.PounceBlockedKnockback, ent.Comp.PounceBlockedKnockbackSpeed, true);

        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-pounce-obstacle", ("lizard", ent.Owner), ("target", target)), ent.Owner, PopupType.MediumCaution);
        return true;
    }
}
