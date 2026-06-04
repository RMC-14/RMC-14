using Content.Shared.Atmos;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Popups;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void OnIgnited(Entity<RMCGiantLizardComponent> ent, ref IgnitedEvent args)
    {
        StartFirePanicRetreat(ent);
    }

    private bool TryFirePanic(Entity<RMCGiantLizardComponent> ent)
    {
        if (!FlammableQuery.TryComp(ent.Owner, out var flammable) || !flammable.OnFire)
            return false;

        StartFirePanicRetreat(ent);

        return true;
    }

    private void StartFirePanicRetreat(Entity<RMCGiantLizardComponent> ent)
    {
        if (ActorQuery.HasComp(ent.Owner) ||
            !MobState.IsAlive(ent.Owner) ||
            ent.Comp.Leaping ||
            ent.Comp.NextFirePanicAt > Timing.CurTime ||
            !FlammableQuery.TryComp(ent.Owner, out var flammable) ||
            !flammable.OnFire)
        {
            return;
        }

        WakeRest(ent);
        ClearAggression(ent);

        if (ent.Comp.Skirmishing)
            StopSkirmish(ent);

        if (ent.Comp.FoodTarget != null || ent.Comp.EatingFood)
            LoseFoodTarget(ent);

        ClearRavage(ent.Comp);

        if (ent.Comp.Retreating)
            return;

        ent.Comp.Retreating = true;
        ent.Comp.RetreatTarget = null;
        ent.Comp.RetreatUntil = Timing.CurTime + ent.Comp.FirePanicDuration;
        ent.Comp.NextRetreatMoveAt = TimeSpan.Zero;
        ent.Comp.NextFirePanicAt = ent.Comp.RetreatUntil;

        _audio.PlayPvs(ent.Comp.HissSound, ent.Owner);
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-fire-panic", ("lizard", ent.Owner)), ent.Owner, PopupType.MediumCaution);
    }

    private bool TryResistFire(Entity<RMCGiantLizardComponent> ent)
    {
        if (!FlammableQuery.TryComp(ent.Owner, out var flammable) || !flammable.OnFire)
            return false;

        _rmcFlammable.DoStopDropRollAnimation(ent.Owner);
        _rmcFlammable.Pat((ent.Owner, flammable), ent.Comp.FireResistStacks);
        Stun.TryKnockdown(ent.Owner, ent.Comp.FireResistStun, true);
        Stun.TryStun(ent.Owner, ent.Comp.FireResistStun, true);
        ent.Comp.NextFirePanicAt = Timing.CurTime + ent.Comp.FirePanicCooldown;

        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-fire-roll", ("lizard", ent.Owner)), ent.Owner, PopupType.MediumCaution);
        return true;
    }
}
