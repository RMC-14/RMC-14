using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Popups;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool TryBlockPounce(Entity<RMCGiantLizardComponent> ent, EntityUid blocker, RMCLeapProtectionComponent protection)
    {
        if (!protection.FullProtection &&
            !_directionalBlock.IsFacingTarget(blocker, ent.Owner, ent.Comp.PounceOrigin))
        {
            return false;
        }

        StopPounce(ent);

        var stun = protection.InherentStunDuration ?? protection.StunDuration;
        if (stun > TimeSpan.Zero)
        {
            Stun.TryKnockdown(ent.Owner, stun, true);
            Stun.TryStun(ent.Owner, stun, true);
        }
        else
        {
            Stun.TryKnockdown(ent.Owner, ent.Comp.PounceBlockedKnockdown, true);
        }

        _size.KnockBack(ent.Owner, Transform.GetMapCoordinates(blocker), ent.Comp.PounceBlockedKnockback, ent.Comp.PounceBlockedKnockback, ent.Comp.PounceBlockedKnockbackSpeed, true);
        _audio.PlayPvs(protection.InherentStunDuration != null ? protection.InherentBlockSound : protection.BlockSound, ent.Owner);

        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-pounce-blocked", ("lizard", ent.Owner), ("target", blocker)), ent.Owner, PopupType.MediumCaution);
        return true;
    }
}
