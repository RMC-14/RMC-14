using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.StatusEffect;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    private void UpdateStatusRecovery(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.NextStatusRecoveryAt > Timing.CurTime)
            return;

        ent.Comp.NextStatusRecoveryAt = Timing.CurTime + ent.Comp.StatusRecoveryCooldown;

        if (!TryComp<StatusEffectsComponent>(ent.Owner, out var status))
            return;

        var recovered = false;
        recovered |= _status.TryRemoveStatusEffect(ent.Owner, "Stun", status);
        recovered |= _status.TryRemoveStatusEffect(ent.Owner, "KnockedDown", status);

        if (!recovered)
            return;

        WakeRest(ent);

        if (ent.Comp.Retreating && !IsOnFire(ent.Owner))
        {
            ent.Comp.Retreating = false;
            ent.Comp.RetreatTarget = null;
            ent.Comp.RetreatAttempts = 0;
            StopMovement(ent.Owner);
        }

        UpdateLizardVisuals(ent);
    }
}
