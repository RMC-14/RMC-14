using Content.Server.Chat.Systems;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Actions;
using Content.Shared.Chat.Prototypes;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Nutrition.Components;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private static readonly ProtoId<EmotePrototype> FlickTongueEmote = "RMCGiantLizardFlickTongue";

    private void OnMapInit(Entity<RMCGiantLizardComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.PounceActionEntity, ent.Comp.PounceAction, ent.Owner);
        ent.Comp.NextUpdateAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.UpdateCooldown);
        ent.Comp.NextTongueFlickAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.TongueFlickCooldown);
        ent.Comp.NextRestCheckAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.RestCheckCooldown);
        ent.Comp.NextRestHealAt = Timing.CurTime + ent.Comp.RestHealCooldown;
        ent.Comp.NextStatusRecoveryAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.StatusRecoveryCooldown);
        UpdateLizardVisuals(ent);
    }

    private void OnShutdown(Entity<RMCGiantLizardComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Comp.PounceActionEntity);
    }

    private void OnDamageChanged(Entity<RMCGiantLizardComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        ent.Comp.LastHitAt = Timing.CurTime;
        WakeRest(ent);
        TryStartBleedTrail(ent, args.DamageDelta);
        UpdateLizardVisuals(ent);

        if (args.Origin is not { } origin || origin == ent.Owner)
            return;

        TryAggro(ent.Owner, origin, ent.Comp);
        PlayGrowl(ent);
        AlertPack(ent.Owner, origin, ent.Comp);
        TryStartFightOrFlight(ent, origin);
    }

    private void OnInteractHand(Entity<RMCGiantLizardComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled ||
            args.User == ent.Owner ||
            !MobState.IsAlive(ent.Owner) ||
            IsOnFire(ent.Owner) ||
            !Faction.IsEntityFriendly(ent.Owner, args.User))
        {
            return;
        }

        if (!ent.Comp.Resting)
        {
            AddRestChance(ent.Comp, ent.Comp.FriendlyPetRestChanceBonus);
            return;
        }

        if (ent.Comp.NextFriendlyPetEmoteAt > Timing.CurTime)
            return;

        ent.Comp.NextFriendlyPetEmoteAt = Timing.CurTime + RandomTime(ent.Comp.FriendlyPetEmoteCooldownMin, ent.Comp.FriendlyPetEmoteCooldownMax);
        Popup.PopupEntity(
            Loc.GetString(PickFriendlyPetPopup(), ("lizard", ent.Owner), ("user", args.User)),
            ent.Owner);

        if (!Random.Prob(ent.Comp.FriendlyPetHissChance))
            return;

        _audio.PlayPvs(ent.Comp.HissSound, ent.Owner);
        ShowTongueFlick(ent);
    }

    private void OnInteractUsing(Entity<RMCGiantLizardComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !IsAcceptedLizardFood(args.Used))
            return;

        WakeRest(ent);
        HealFraction(ent.Owner, ent.Comp.DirectFeedHealFraction);
        TryTameToFeeder(ent.Owner, args.User, ent.Comp);
        QueueDel(args.Used);
        args.Handled = true;

        UpdateLizardVisuals(ent);
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-feed"), ent.Owner, args.User);
    }

    private void OnDisarmed(Entity<RMCGiantLizardComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled ||
            args.Target != ent.Owner ||
            !MobState.IsAlive(ent.Owner) ||
            ent.Comp.Leaping ||
            !Random.Prob(ent.Comp.DisarmKnockdownChance))
        {
            return;
        }

        args.Handled = true;
        args.IsStunned = true;
        WakeRest(ent);
        Stun.TryKnockdown(ent.Owner, ent.Comp.DisarmKnockdown, true);
        _audio.PlayPvs(ent.Comp.DisarmKnockdownSound, ent.Owner);
        Popup.PopupEntity(Loc.GetString("rmc-giant-lizard-disarmed", ("lizard", ent.Owner), ("user", args.Source)), ent.Owner);
        UpdateLizardVisuals(ent);
    }

    private void OnMobStateChanged(Entity<RMCGiantLizardComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateLizardVisuals(ent);
    }

    private void OnPounceAction(Entity<RMCGiantLizardComponent> ent, ref RMCGiantLizardPounceActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryPounce(ent, args.Target);
    }

    private void OnStartCollide(Entity<RMCGiantLizardComponent> ent, ref StartCollideEvent args)
    {
        if (!ent.Comp.Leaping)
            return;

        if (TryApplyPounceHit(ent, args.OtherEntity))
            return;

        TryApplyPounceObjectHit(ent, args.OtherEntity);
    }

    private void OnPhysicsSleep(Entity<RMCGiantLizardComponent> ent, ref PhysicsSleepEvent args)
    {
        if (ent.Comp.Leaping)
            StopPounce(ent);
    }

    private void OnMeleeHit(Entity<RMCGiantLizardComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.User != ent.Owner || args.HitEntities.Count == 0)
            return;

        args.HitSoundOverride = Random.Prob(0.5f) ? ent.Comp.SlashAttackSound : ent.Comp.BiteAttackSound;

        EntityUid? firstLivingTarget = null;
        foreach (var target in args.HitEntities)
        {
            if (target == ent.Owner || !ValidLizardTarget(target))
                continue;

            firstLivingTarget ??= target;
            TryAggro(ent.Owner, target, ent.Comp);
        }

        if (firstLivingTarget is not { } livingTarget)
            return;

        if (TryStartRavage(ent, livingTarget, true))
        {
            args.Handled = true;
            return;
        }

        if (HasComp<XenoComponent>(livingTarget))
            args.BonusDamage += ent.Comp.MeleeXenoBonusDamage;

        if (!Random.Prob(ent.Comp.SkirmishChance))
            return;

        StartSkirmish(ent, livingTarget);
    }

    private void OnEmote(Entity<RMCGiantLizardComponent> ent, ref EmoteEvent args)
    {
        if (args.Emote.ID != FlickTongueEmote)
            return;

        ShowTongueFlick(ent);
        args.Handled = true;
    }

    private void OnFoodPickedUp(Entity<FoodComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!IsAcceptedLizardFood(ent.Owner))
            return;

        var holderCoords = Transform.GetMapCoordinates(args.User);
        foreach (var lizard in Lookup.GetEntitiesInRange<RMCGiantLizardComponent>(holderCoords, 8f))
        {
            if (lizard.Comp.FoodTarget != ent.Owner ||
                ActorQuery.HasComp(lizard.Owner) ||
                !MobState.IsAlive(lizard.Owner))
            {
                continue;
            }

            TryHandleFoodHolder((lizard.Owner, lizard.Comp), ent.Owner);
        }
    }

    private void OnFoodDropped(Entity<FoodComponent> ent, ref GotUnequippedHandEvent args)
    {
        _lastFoodHolder[ent.Owner] = args.User;
    }

    private void OnFoodTerminating(Entity<FoodComponent> ent, ref EntityTerminatingEvent args)
    {
        _lastFoodHolder.Remove(ent.Owner);
    }
}
