using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCCatHuntingSystem
{
    private EntityUid? PickPrey(Entity<RMCCatHunterComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var prey in Lookup.GetEntitiesInRange<RMCAnimalPreyComponent>(mapCoords, ent.Comp1.SearchRange))
        {
            if (prey.Owner == ent.Owner || !ValidLivingMob(prey.Owner))
                continue;

            if (ent.Comp1.PreyWhitelist != null && _whitelist.IsWhitelistFail(ent.Comp1.PreyWhitelist, prey.Owner))
                continue;

            var preyCoords = Transform.GetMapCoordinates(prey.Owner);
            var distance = (preyCoords.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = prey.Owner;
            bestDistance = distance;
        }

        return best;
    }

    private void AttackPrey(EntityUid cat, EntityUid prey, RMCCatHunterComponent hunter)
    {
        hunter.PlayCounter++;

        Popup.PopupEntity(Loc.GetString(PickCatAttackPopup(), ("cat", cat), ("prey", prey)), cat);
        _audio.PlayPvs(hunter.HuntHitSound, cat);

        var damage = ActorQuery.HasComp(prey)
            ? hunter.PlayerPreyDamage
            : hunter.NpcPreyDamage;

        Damageable.TryChangeDamage(prey, damage, origin: cat, tool: cat);
        Stun.TryKnockdown(prey, hunter.PlayerPreyKnockdown, true);
        Stun.TrySlowdown(prey, hunter.PlayerPreySlowdown, true, 0.3f, 0.3f);
    }

    private void TryThreatenPrey(EntityUid cat, EntityUid prey, RMCCatHunterComponent hunter, float distance, TimeSpan now)
    {
        if (distance > hunter.ThreatenRange ||
            hunter.NextThreatenAt > now ||
            !Random.Prob(hunter.ThreatenChance))
        {
            return;
        }

        hunter.NextThreatenAt = now + hunter.ThreatenCooldown;
        Popup.PopupEntity(Loc.GetString(PickCatThreatenPopup(), ("cat", cat), ("prey", prey)), cat);
    }

    private string PickCatAttackPopup()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-cat-bites-prey",
            1 => "rmc-cat-toys-prey",
            _ => "rmc-cat-chomps-prey",
        };
    }

    private string PickCatThreatenPopup()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-cat-hisses-at",
            1 => "rmc-cat-mrowls",
            _ => "rmc-cat-eyes-hungrily",
        };
    }
}
