using System.Linq;
using System.Numerics;
using Content.Server._RMC14.Atmos;
using Content.Server._RMC14.Barricade;
using Content.Server._RMC14.NPC;
using Content.Shared._RMC14.Barricade;
using Content.Shared._RMC14.CameraShake;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Slow;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Vents;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Physics;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Projectiles;
using Content.Shared.Spider;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed class RMCParrotSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCParrotComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCParrotComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<RMCParrotComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCParrotComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCParrotComponent, ProjectileDamageDealtEvent>(OnProjectileDamageDealt);
    }

    private void OnMapInit(Entity<RMCParrotComponent> ent, ref MapInitEvent args)
    {
        Container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
        SetPerched(ent, false);
    }

    private void OnShutdown(Entity<RMCParrotComponent> ent, ref ComponentShutdown args)
    {
        DropHeldItem(ent);
    }

    private void OnMobStateChanged(Entity<RMCParrotComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == Content.Shared.Mobs.MobState.Dead)
            DropHeldItem(ent);
    }

    private void OnDamageChanged(Entity<RMCParrotComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased ||
            args.DamageDelta == null ||
            args.DamageDelta.Empty)
            return;

        WakeParrot(ent);

        if (ActorQuery.HasComp(ent.Owner))
            return;

        if (args.Tool is { } tool && HasComp<ProjectileComponent>(tool))
            return;

        if (args.Origin is { } origin && origin != ent.Owner && ValidLivingMob(origin))
            StartDefensiveBehavior(ent, origin);
        else
            StartPanic(ent, args.Origin);
    }

    private void OnProjectileDamageDealt(Entity<RMCParrotComponent> ent, ref ProjectileDamageDealtEvent args)
    {
        if (args.DamageDelta == null || args.DamageDelta.Empty)
            return;

        WakeParrot(ent);

        if (ActorQuery.HasComp(ent.Owner))
            return;

        StartPanic(ent, args.Origin);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCParrotComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var parrot, out var xform))
        {
            if (!MobState.IsAlive(uid))
                continue;

            if (ActorQuery.HasComp(uid))
            {
                ClearParrotBehavior((uid, parrot), false);
                WakeParrot((uid, parrot));
                continue;
            }

            if (UpdateDefensiveBehavior((uid, parrot, xform), now))
                continue;

            if (parrot.NextThinkAt > now)
                continue;

            parrot.NextThinkAt = now + parrot.ThinkCooldown;

            if (parrot.Perched)
            {
                if (Random.Prob(parrot.WakeChance))
                    WakeParrot((uid, parrot));

                continue;
            }

            if (ValidateHeldItem((uid, parrot)))
            {
                UpdateHeldItemReturn((uid, parrot, xform));
                continue;
            }

            if (!Random.Prob(parrot.StealChance))
                continue;

            if (TryStealGroundItem((uid, parrot, xform)))
                continue;

            TryStealHeldItem((uid, parrot, xform));
        }
    }

    private bool UpdateDefensiveBehavior(Entity<RMCParrotComponent, TransformComponent> ent, TimeSpan now)
    {
        switch (ent.Comp1.Behavior)
        {
            case RMCParrotBehavior.None:
                return false;
            case RMCParrotBehavior.Panic:
                return UpdatePanic(ent, now);
            case RMCParrotBehavior.Flee:
                return UpdateFlee(ent, now);
            case RMCParrotBehavior.Attack:
                return UpdateAttack(ent, now);
            default:
                ClearParrotBehavior((ent.Owner, ent.Comp1));
                return false;
        }
    }

    private bool UpdatePanic(Entity<RMCParrotComponent, TransformComponent> ent, TimeSpan now)
    {
        if (now >= ent.Comp1.BehaviorUntil)
        {
            ClearParrotBehavior((ent.Owner, ent.Comp1));
            return false;
        }

        if (ent.Comp1.BehaviorTarget is { } target &&
            ValidLivingMob(target) &&
            XformQuery.TryGetComponent(target, out var targetXform))
        {
            TryMoveAwayFrom(ent.Owner, targetXform.Coordinates, ent.Comp1.PanicFlySpeed);
            return true;
        }

        if (ent.Comp1.NextPanicMoveAt <= now)
        {
            ent.Comp1.NextPanicMoveAt = now + RandomTime(ent.Comp1.PanicMoveCooldownMin, ent.Comp1.PanicMoveCooldownMax);
            TryMoveRandomly(ent.Owner, ent.Comp1.PanicFlySpeed);
        }

        return true;
    }

    private bool UpdateFlee(Entity<RMCParrotComponent, TransformComponent> ent, TimeSpan now)
    {
        if (now >= ent.Comp1.BehaviorUntil ||
            ent.Comp1.BehaviorTarget is not { } target ||
            !ValidLivingMob(target) ||
            !XformQuery.TryGetComponent(target, out var targetXform))
        {
            ClearParrotBehavior((ent.Owner, ent.Comp1));
            return false;
        }

        TryMoveAwayFrom(ent.Owner, targetXform.Coordinates, ent.Comp1.FleeSpeed);
        return true;
    }

    private bool UpdateAttack(Entity<RMCParrotComponent, TransformComponent> ent, TimeSpan now)
    {
        if (now >= ent.Comp1.BehaviorUntil ||
            ent.Comp1.BehaviorTarget is not { } target ||
            !ValidLivingMob(target) ||
            MobState.IsIncapacitated(target) ||
            !XformQuery.TryGetComponent(target, out var targetXform))
        {
            ClearParrotBehavior((ent.Owner, ent.Comp1));
            return false;
        }

        var targetCoords = targetXform.Coordinates;
        if (!Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, targetCoords, out var distance))
        {
            ClearParrotBehavior((ent.Owner, ent.Comp1));
            return false;
        }

        if (distance > ent.Comp1.AttackRange)
        {
            TryMoveTowards(ent.Owner, targetCoords, ent.Comp1.AttackFlySpeed);
            return true;
        }

        StopMovement(ent.Owner);
        if (ent.Comp1.NextAttackAt > now)
            return true;

        ent.Comp1.NextAttackAt = now + ent.Comp1.AttackCooldown;
        var damage = new DamageSpecifier
        {
            DamageDict =
            {
                { "Slash", FixedPoint2.New(Random.NextFloat(ent.Comp1.AttackDamageMin, ent.Comp1.AttackDamageMax)) },
            },
        };

        Damageable.TryChangeDamage(target, damage, origin: ent.Owner, tool: ent.Owner);
        Popup.PopupEntity(Loc.GetString(PickParrotAttackPopup(), ("parrot", ent.Owner), ("target", target)), ent.Owner);
        return true;
    }

    private void StartDefensiveBehavior(Entity<RMCParrotComponent> ent, EntityUid attacker)
    {
        WakeParrot(ent);

        if (IsWeakTarget(attacker, ent.Comp.WeakTargetDamageFraction))
        {
            ent.Comp.Behavior = RMCParrotBehavior.Attack;
            ent.Comp.BehaviorTarget = attacker;
            ent.Comp.BehaviorUntil = Timing.CurTime + ent.Comp.AttackDuration;
            ent.Comp.NextAttackAt = Timing.CurTime;
            ent.Comp.NextThinkAt = ent.Comp.BehaviorUntil;
            Popup.PopupEntity(Loc.GetString("rmc-parrot-turns-on", ("parrot", ent.Owner), ("target", attacker)), ent.Owner);
            return;
        }

        DropHeldItem(ent);
        ent.Comp.Behavior = RMCParrotBehavior.Flee;
        ent.Comp.BehaviorTarget = attacker;
        ent.Comp.BehaviorUntil = Timing.CurTime + ent.Comp.FleeDuration;
        ent.Comp.NextThinkAt = ent.Comp.BehaviorUntil;
        Popup.PopupEntity(Loc.GetString("rmc-parrot-flees", ("parrot", ent.Owner), ("target", attacker)), ent.Owner);
    }

    private void StartPanic(Entity<RMCParrotComponent> ent, EntityUid? threat)
    {
        WakeParrot(ent);
        DropHeldItem(ent);

        ent.Comp.Behavior = RMCParrotBehavior.Panic;
        ent.Comp.BehaviorTarget = threat;
        ent.Comp.BehaviorUntil = Timing.CurTime + ent.Comp.ShotPanicDuration;
        ent.Comp.NextPanicMoveAt = TimeSpan.Zero;
        ent.Comp.NextThinkAt = ent.Comp.BehaviorUntil;
        Popup.PopupEntity(Loc.GetString("rmc-parrot-panics", ("parrot", ent.Owner)), ent.Owner);
    }

    private void ClearParrotBehavior(Entity<RMCParrotComponent> ent, bool stopMovement = true)
    {
        if (ent.Comp.Behavior == RMCParrotBehavior.None)
            return;

        ent.Comp.Behavior = RMCParrotBehavior.None;
        ent.Comp.BehaviorTarget = null;
        ent.Comp.BehaviorUntil = TimeSpan.Zero;

        if (stopMovement)
            StopMovement(ent.Owner);
    }

    private bool IsWeakTarget(EntityUid target, float weakDamageFraction)
    {
        const float missingThresholdWeakDamage = 50f;

        if (MobState.IsIncapacitated(target))
            return true;

        if (!DamageableQuery.TryComp(target, out var damageable) ||
            !ThresholdsQuery.TryComp(target, out var thresholds))
        {
            return false;
        }

        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state != Content.Shared.Mobs.MobState.Dead || threshold <= FixedPoint2.Zero)
                continue;

            return damageable.TotalDamage.Float() / threshold.Float() >= weakDamageFraction;
        }

        return damageable.TotalDamage.Float() >= missingThresholdWeakDamage;
    }

    private string PickParrotAttackPopup()
    {
        return Random.Next(2) == 0
            ? "rmc-parrot-pecks"
            : "rmc-parrot-claws";
    }

    private bool ValidateHeldItem(Entity<RMCParrotComponent> ent)
    {
        if (ent.Comp.HeldItem is not { } held || !TerminatingOrDeleted(held))
            return ent.Comp.HeldItem != null;

        ent.Comp.HeldItem = null;
        return false;
    }

    private void UpdateHeldItemReturn(Entity<RMCParrotComponent, TransformComponent> ent)
    {
        if (ent.Comp1.Perch == null || TerminatingOrDeleted(ent.Comp1.Perch.Value))
            ent.Comp1.Perch = PickPerch(ent);

        if (ent.Comp1.Perch is not { } perch || !XformQuery.TryGetComponent(perch, out var perchXform))
        {
            DropHeldItem((ent.Owner, ent.Comp1));
            return;
        }

        var perchCoords = perchXform.Coordinates;
        if (Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, perchCoords, out var distance) &&
            distance <= ent.Comp1.PerchArriveRange)
        {
            DropHeldItem((ent.Owner, ent.Comp1), perchCoords);
            SetPerched((ent.Owner, ent.Comp1), true);
            Popup.PopupEntity(Loc.GetString("rmc-parrot-perches", ("parrot", ent.Owner)), ent.Owner);
            return;
        }

        TryMoveTowards(ent.Owner, perchCoords, ent.Comp1.FlySpeed);
    }

    private bool TryStealGroundItem(Entity<RMCParrotComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var item in Lookup.GetEntitiesInRange<ItemComponent>(mapCoords, ent.Comp1.SearchRange))
        {
            if (!CanStealItem(ent.Comp1, item))
                continue;

            if (Container.IsEntityOrParentInContainer(item.Owner))
                continue;

            var itemCoords = Transform.GetMapCoordinates(item.Owner);
            var distance = (itemCoords.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = item.Owner;
            bestDistance = distance;
        }

        if (best == null)
            return false;

        var targetCoords = Transform.GetMoverCoordinates(best.Value);
        if (bestDistance > ent.Comp1.PickupRange)
        {
            TryMoveTowards(ent.Owner, targetCoords, ent.Comp1.FlySpeed);
            return true;
        }

        return TryStoreItem((ent.Owner, ent.Comp1), best.Value);
    }

    private bool TryStealHeldItem(Entity<RMCParrotComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        foreach (var hands in Lookup.GetEntitiesInRange<HandsComponent>(mapCoords, ent.Comp1.SearchRange))
        {
            if (hands.Owner == ent.Owner ||
                !MobState.IsAlive(hands.Owner) ||
                !_hands.TryGetActiveItem((hands.Owner, hands.Comp), out var held) ||
                held == null ||
                !ItemQuery.TryComp(held.Value, out var item) ||
                !CanStealItem(ent.Comp1, (held.Value, item)))
            {
                continue;
            }

            var ownerCoords = Transform.GetMoverCoordinates(hands.Owner);
            if (!Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, ownerCoords, out var distance))
                continue;

            if (distance > ent.Comp1.PickupRange)
            {
                TryMoveTowards(ent.Owner, ownerCoords, ent.Comp1.FlySpeed);
                return true;
            }

            if (!_hands.TryDrop((hands.Owner, hands.Comp), held.Value, Transform.GetMoverCoordinates(ent.Owner), false, false))
                continue;

            if (TryStoreItem((ent.Owner, ent.Comp1), held.Value))
                return true;
        }

        return false;
    }

    private bool TryStoreItem(Entity<RMCParrotComponent> ent, EntityUid item)
    {
        if (!ItemQuery.TryComp(item, out var itemComp) ||
            !CanStealItem(ent.Comp, (item, itemComp)) ||
            !Container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
        {
            return false;
        }

        if (!Container.Insert((item, null, null, null), container, force: true))
            return false;

        ent.Comp.HeldItem = item;
        WakeParrot(ent);
        Popup.PopupEntity(Loc.GetString("rmc-parrot-steals", ("parrot", ent.Owner), ("item", item)), ent.Owner);
        return true;
    }

    private bool CanStealItem(RMCParrotComponent parrot, Entity<ItemComponent> item)
    {
        return item.Owner.Valid &&
               !TerminatingOrDeleted(item.Owner) &&
               parrot.HeldItem == null &&
               parrot.StolenSizes.Contains(item.Comp.Size);
    }

    private EntityUid? PickPerch(Entity<RMCParrotComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var surface in Lookup.GetEntitiesInRange<PlaceableSurfaceComponent>(mapCoords, ent.Comp1.PerchRange))
        {
            if (surface.Owner == ent.Owner ||
                ent.Comp1.PerchWhitelist != null && _whitelist.IsWhitelistFail(ent.Comp1.PerchWhitelist, surface.Owner) ||
                !XformQuery.TryGetComponent(surface.Owner, out var surfaceXform) ||
                !surfaceXform.Anchored)
            {
                continue;
            }

            var distance = (Transform.GetMapCoordinates((surface.Owner, surfaceXform)).Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = surface.Owner;
            bestDistance = distance;
        }

        return best;
    }

    private void DropHeldItem(Entity<RMCParrotComponent> ent, EntityCoordinates? coordinates = null)
    {
        if (!Container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        var destination = coordinates ?? Transform(ent.Owner).Coordinates;
        Container.EmptyContainer(container, true, destination);
        ent.Comp.HeldItem = null;
    }

    private void SetPerched(Entity<RMCParrotComponent> ent, bool perched)
    {
        ent.Comp.Perched = perched;
        _appearance.SetData(ent.Owner, RMCParrotVisuals.Perched, perched);

        if (perched)
        {
            StopMovement(ent.Owner);
            RMCNpc.SleepNPC(ent.Owner);
        }
        else
        {
            RMCNpc.WakeNPC(ent.Owner);
        }
    }

    private void WakeParrot(Entity<RMCParrotComponent> ent)
    {
        if (!ent.Comp.Perched)
            return;

        SetPerched(ent, false);
    }
}
