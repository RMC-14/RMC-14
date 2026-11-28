using Content.Client.Gameplay;
using Content.Shared._RMC14.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.State;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Weapons.Ranged;

public sealed class RMCGunSystem : EntitySystem
{
    [Dependency] private readonly IStateManager _stateManager = default!;

    public EntityUid? GetAttackTarget(EntityUid attacker, MapCoordinates mousePos, Entity<GunComponent> gun)
    {
        if (_stateManager.CurrentState is not GameplayStateBase screen)
            return null;

        var clickables = screen.GetClickableEntities(mousePos);

        EntityUid? firstTarget = null;
        EntityUid? firstPriorityTarget = null;

        foreach (var clickable in clickables)
        {
            var gunEv = new CheckShotTargetEvent(clickable, attacker);
            RaiseLocalEvent(gun, ref gunEv);

            // Gun decided target should be skipped
            if (gunEv.Skip)
                continue;

            var attackerEv = new CheckGunTargetEvent(clickable, gun);
            RaiseLocalEvent(attacker, ref attackerEv);

            // Attacker decided target should be skipped
            if (attackerEv.Skip)
                continue;

            var targetEv = new CheckGunAttackerEvent(attacker, gun);
            RaiseLocalEvent(clickable, ref targetEv);

            // Target decided it should be skipped
            if (targetEv.Skip)
                continue;

            firstTarget ??= clickable;

            // Either attacker or target decided it should be deferred
            if (gunEv.Defer || attackerEv.Defer || targetEv.Defer)
                continue;

            firstPriorityTarget ??= clickable;
            break;
        }

        return firstPriorityTarget ?? firstTarget;
    }
}
