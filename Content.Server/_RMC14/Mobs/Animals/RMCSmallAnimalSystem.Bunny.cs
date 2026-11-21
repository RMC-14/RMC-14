using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCSmallAnimalSystem
{
    private void OnBunnyMapInit(Entity<RMCBunnyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
    }

    private void UpdateBunnies(TimeSpan now)
    {
        var query = EntityQueryEnumerator<RMCBunnyComponent>();
        while (query.MoveNext(out var uid, out var bunny))
        {
            if (!MobState.IsAlive(uid) ||
                ActorQuery.HasComp(uid) ||
                Container.IsEntityInContainer(uid) ||
                bunny.NextThinkAt > now)
            {
                continue;
            }

            bunny.NextThinkAt = now + bunny.ThinkCooldown;
            TryBunnyAmbientEmote((uid, bunny));
        }
    }

    private void OnBunnyInteractHand(Entity<RMCBunnyComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || args.User == ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-bunny-pet", ("user", args.User), ("bunny", ent.Owner)), ent.Owner);
    }

    private void OnBunnyDisarmed(Entity<RMCBunnyComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled || args.Target != ent.Owner || !MobState.IsAlive(ent.Owner))
            return;

        args.Handled = true;
        Popup.PopupEntity(Loc.GetString("rmc-bunny-push", ("user", args.Source), ("bunny", ent.Owner)), ent.Owner);

        if (!XformQuery.HasComp(args.Source))
            return;

        _size.KnockBack(ent.Owner,
            Transform.GetMapCoordinates(args.Source),
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockback,
            ent.Comp.ShooKnockbackSpeed,
            true);
    }

    private void OnBunnyDamageChanged(Entity<RMCBunnyComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.Origin is not { } origin ||
            origin == ent.Owner ||
            !ActorQuery.HasComp(origin) ||
            !MobQuery.HasComp(origin) ||
            ent.Comp.NextKickPopupAt > Timing.CurTime)
        {
            return;
        }

        var bunnyCoords = Transform.GetMoverCoordinates(ent.Owner);
        var originCoords = Transform.GetMoverCoordinates(origin);
        if (!bunnyCoords.TryDistance(EntityManager, originCoords, out var distance) || distance > 1.75f)
            return;

        ent.Comp.NextKickPopupAt = Timing.CurTime + ent.Comp.KickPopupCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-bunny-kick", ("user", origin), ("bunny", ent.Owner)), ent.Owner);
    }

    private void TryBunnyAmbientEmote(Entity<RMCBunnyComponent> ent)
    {
        if (Random.Prob(ent.Comp.HeardEmoteChance))
        {
            Popup.PopupEntity(Loc.GetString(PickBunnyHeardEmote(), ("bunny", ent.Owner)), ent.Owner);
            return;
        }

        if (!Random.Prob(ent.Comp.SeenEmoteChance))
            return;

        Popup.PopupEntity(Loc.GetString(PickBunnySeenEmote(), ("bunny", ent.Owner)), ent.Owner);
    }

    private string PickBunnyHeardEmote()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-bunny-purrs",
            1 => "rmc-bunny-hums",
            _ => "rmc-bunny-squeaks",
        };
    }

    private string PickBunnySeenEmote()
    {
        return Random.Next(2) == 0
            ? "rmc-bunny-flaps-ears"
            : "rmc-bunny-sniffs";
    }
}
