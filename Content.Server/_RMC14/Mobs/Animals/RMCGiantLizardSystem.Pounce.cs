using System.Numerics;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void UpdatePounce(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var now = Timing.CurTime;
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, 0.9f))
        {
            if (TryApplyPounceHit((ent.Owner, ent.Comp1), mob.Owner))
                return;
        }

        if (ent.Comp1.PounceEndAt <= now)
            StopPounce((ent.Owner, ent.Comp1));
    }

    private bool TryPounce(Entity<RMCGiantLizardComponent> ent, EntityCoordinates destination)
    {
        var now = Timing.CurTime;
        if (ent.Comp.Leaping)
        {
            PopupPounceFailure(ent, "rmc-giant-lizard-pounce-already");
            return false;
        }

        if (ent.Comp.NextPounceAt > now)
        {
            var seconds = (int) Math.Ceiling((ent.Comp.NextPounceAt - now).TotalSeconds);
            PopupPounceFailure(ent, "rmc-giant-lizard-pounce-cooldown", ("seconds", seconds));
            return false;
        }

        if (!PhysicsQuery.TryComp(ent.Owner, out var physics))
            return false;

        var origin = Transform.GetMoverCoordinates(ent.Owner);
        var originMap = Transform.ToMapCoordinates(origin);
        var destinationMap = Transform.ToMapCoordinates(destination, false);
        if (originMap.MapId != destinationMap.MapId)
        {
            PopupPounceFailure(ent, "rmc-giant-lizard-pounce-range");
            return false;
        }

        var direction = destinationMap.Position - originMap.Position;
        var distance = direction.Length();
        if (distance < ent.Comp.MinPounceRange ||
            distance > ent.Comp.MaxPounceRange + 0.25f)
        {
            PopupPounceFailure(ent, "rmc-giant-lizard-pounce-range");
            return false;
        }

        if (direction.LengthSquared() < 0.01f)
        {
            PopupPounceFailure(ent, "rmc-giant-lizard-pounce-range");
            return false;
        }

        WakeRest(ent);
        StopRoam(ent, false);

        ent.Comp.NextPounceAt = now + ent.Comp.PounceCooldown;
        ent.Comp.Leaping = true;
        ent.Comp.PounceOrigin = origin;
        ent.Comp.PounceEndAt = now + TimeSpan.FromSeconds(distance / Math.Max(1, ent.Comp.PounceStrength));

        Physics.ResetDynamics(ent.Owner, physics);
        Physics.ApplyLinearImpulse(ent.Owner, direction.Normalized() * ent.Comp.PounceStrength * physics.Mass, body: physics);
        Physics.SetBodyStatus(ent.Owner, physics, BodyStatus.InAir);
        return true;
    }

    private void StopPounce(Entity<RMCGiantLizardComponent> ent)
    {
        ent.Comp.Leaping = false;
        ent.Comp.PounceTarget = null;
        StopMovement(ent.Owner);

        if (PhysicsQuery.TryComp(ent.Owner, out var physics))
            Physics.SetBodyStatus(ent.Owner, physics, BodyStatus.OnGround);
    }

    private void PopupPounceFailure(Entity<RMCGiantLizardComponent> ent, string locId, params (string, object)[] args)
    {
        if (!ActorQuery.HasComp(ent.Owner))
            return;

        Popup.PopupEntity(Loc.GetString(locId, args), ent.Owner, ent.Owner, PopupType.SmallCaution);
    }
}
