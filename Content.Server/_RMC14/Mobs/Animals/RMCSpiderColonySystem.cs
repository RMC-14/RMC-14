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

public sealed class RMCSpiderColonySystem : RMCAnimalSystem
{
    [Dependency] private readonly RMCDazedSystem _dazed = default!;
    [Dependency] private readonly RMCSlowSystem _slow = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSpiderNestMemberComponent, MapInitEvent>(OnSpiderNestMemberMapInit);
        SubscribeLocalEvent<RMCSpiderNestMemberComponent, MobStateChangedEvent>(OnSpiderNestMemberMobStateChanged);
        SubscribeLocalEvent<RMCSpiderVenomComponent, MeleeHitEvent>(OnSpiderMeleeHit);
        SubscribeLocalEvent<RMCSpiderWebComponent, PreventCollideEvent>(OnWebPreventCollide);
        SubscribeLocalEvent<RMCSpiderWebComponent, StartCollideEvent>(OnWebStartCollide);
        SubscribeLocalEvent<RMCSpiderNurseComponent, DamageChangedEvent>(OnNurseDamageChanged);
        SubscribeLocalEvent<RMCSpiderNurseComponent, MobStateChangedEvent>(OnNurseMobStateChanged);
        SubscribeLocalEvent<RMCSpiderEggComponent, MapInitEvent>(OnEggMapInit);
        SubscribeLocalEvent<RMCSpiderlingGrowthComponent, MapInitEvent>(OnSpiderlingMapInit);
        SubscribeLocalEvent<RMCSpiderlingGrowthComponent, MobStateChangedEvent>(OnSpiderlingMobStateChanged);
        SubscribeLocalEvent<RMCSpiderCocoonComponent, ComponentInit>(OnCocoonInit);
        SubscribeLocalEvent<RMCSpiderCocoonComponent, ComponentShutdown>(OnCocoonShutdown);
    }

    private void OnSpiderNestMemberMapInit(Entity<RMCSpiderNestMemberComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextIdleSkitterAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.IdleSkitterCooldown);
    }

    private void OnSpiderNestMemberMobStateChanged(Entity<RMCSpiderNestMemberComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == Content.Shared.Mobs.MobState.Alive)
            return;

        StopAdultSpiderSkitter(ent);
    }

    private void OnSpiderMeleeHit(Entity<RMCSpiderVenomComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!ValidLivingMob(target) || HasComp<RMCSpiderNestMemberComponent>(target))
                continue;

            if (Random.Prob(ent.Comp.PrickPopupChance))
                Popup.PopupEntity(Loc.GetString("rmc-spider-venom-prick"), target, target, PopupType.SmallCaution);

            if (ent.Comp.DazeTime > TimeSpan.Zero)
                _dazed.TryDaze(target, ent.Comp.DazeTime, true);
        }
    }

    private void OnWebPreventCollide(Entity<RMCSpiderWebComponent> ent, ref PreventCollideEvent args)
    {
        if (HasComp<RMCSpiderNestMemberComponent>(args.OtherEntity) || HasComp<IgnoreSpiderWebComponent>(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (!HasComp<ProjectileComponent>(args.OtherEntity))
            return;

        if (!Random.Prob(ent.Comp.ProjectileBlockChance))
            args.Cancelled = true;
    }

    private void OnWebStartCollide(Entity<RMCSpiderWebComponent> ent, ref StartCollideEvent args)
    {
        if (HasComp<RMCSpiderNestMemberComponent>(args.OtherEntity) ||
            HasComp<IgnoreSpiderWebComponent>(args.OtherEntity) ||
            !MobQuery.HasComp(args.OtherEntity) ||
            !Random.Prob(ent.Comp.MobRootChance))
        {
            return;
        }

        _slow.TryRoot(args.OtherEntity, ent.Comp.MobRootTime);
        Stun.TrySlowdown(args.OtherEntity, ent.Comp.MobRootTime, true, 0.2f, 0.2f);
    }

    private void OnNurseDamageChanged(Entity<RMCSpiderNurseComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        CancelNurseWork(ent, true);
    }

    private void OnNurseMobStateChanged(Entity<RMCSpiderNurseComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == Content.Shared.Mobs.MobState.Alive)
            return;

        CancelNurseWork(ent, false);
    }

    private void OnEggMapInit(Entity<RMCSpiderEggComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.HatchAt = Timing.CurTime + RandomTime(ent.Comp.HatchMin, ent.Comp.HatchMax);
    }

    private void OnSpiderlingMapInit(Entity<RMCSpiderlingGrowthComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.GrowProgress = !ent.Comp.NoGrow && Random.Prob(ent.Comp.GrowChance) ? 1f : -1f;
        ent.Comp.GrowAt = Timing.CurTime + ent.Comp.GrowStepCooldown;
        ent.Comp.NextSkitterAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.SkitterCooldown);
    }

    private void OnSpiderlingMobStateChanged(Entity<RMCSpiderlingGrowthComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != Content.Shared.Mobs.MobState.Dead || ent.Comp.SpawnedRemains)
            return;

        ent.Comp.SpawnedRemains = true;
        if (XformQuery.TryGetComponent(ent.Owner, out var xform))
        {
            Popup.PopupEntity(Loc.GetString("rmc-spiderling-dies", ("spiderling", ent.Owner)), ent.Owner);
            Spawn(ent.Comp.RemainsPrototype, xform.Coordinates);
        }

        QueueDel(ent.Owner);
    }

    private void OnCocoonInit(Entity<RMCSpiderCocoonComponent> ent, ref ComponentInit args)
    {
        Container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
    }

    private void OnCocoonShutdown(Entity<RMCSpiderCocoonComponent> ent, ref ComponentShutdown args)
    {
        if (!Container.TryGetContainer(ent.Owner, ent.Comp.ContainerId, out var container))
            return;

        EntityCoordinates? destination = null;
        if (XformQuery.TryGetComponent(ent.Owner, out var xform))
        {
            destination = xform.Coordinates;
            if (container.ContainedEntities.Count > 0)
                Popup.PopupEntity(Loc.GetString("rmc-spider-cocoon-splits", ("cocoon", ent.Owner)), ent.Owner);
        }

        Container.EmptyContainer(container, true, destination);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateSpiderPossession();
        UpdateAdultSpiderSkitter();
        UpdateNurses();
        UpdateEggs();
        UpdateSpiderlings();
    }

    private void UpdateSpiderPossession()
    {
        var query = EntityQueryEnumerator<RMCSpiderNestMemberComponent>();
        while (query.MoveNext(out var uid, out var spider))
        {
            if (!MobState.IsAlive(uid))
            {
                spider.SleepingForPossession = false;
                continue;
            }

            if (ActorQuery.HasComp(uid))
            {
                StopAdultSpiderSkitter((uid, spider));

                if (TryComp<RMCSpiderNurseComponent>(uid, out var nurse))
                    CancelNurseWork((uid, nurse), false);

                if (spider.SleepingForPossession)
                    continue;

                StopMovement(uid);
                RMCNpc.SleepNPC(uid);
                spider.SleepingForPossession = true;
                continue;
            }

            if (!spider.SleepingForPossession)
                continue;

            RMCNpc.WakeNPC(uid);
            spider.SleepingForPossession = false;
        }
    }

    private void UpdateAdultSpiderSkitter()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCSpiderNestMemberComponent>();
        while (query.MoveNext(out var uid, out var spider))
        {
            if (HasComp<RMCSpiderlingGrowthComponent>(uid))
                continue;

            if (spider.IdleSkittering)
            {
                if (ActorQuery.HasComp(uid) ||
                    !MobState.IsAlive(uid) ||
                    now >= spider.IdleSkitterUntil)
                {
                    StopAdultSpiderSkitter((uid, spider));
                }

                continue;
            }

            if (spider.NextIdleSkitterAt > now ||
                ActorQuery.HasComp(uid) ||
                !MobState.IsAlive(uid) ||
                HasBusyNurseWork(uid) ||
                !Random.Prob(spider.IdleSkitterChance))
            {
                continue;
            }

            StartAdultSpiderSkitter((uid, spider));
        }
    }

    private bool HasBusyNurseWork(EntityUid uid)
    {
        return TryComp<RMCSpiderNurseComponent>(uid, out var nurse) &&
               nurse.BusyWork != RMCSpiderNurseWork.None;
    }

    private void StartAdultSpiderSkitter(Entity<RMCSpiderNestMemberComponent> ent)
    {
        ent.Comp.IdleSkittering = true;
        ent.Comp.IdleSkitterUntil = Timing.CurTime + ent.Comp.IdleSkitterDuration;
        ent.Comp.NextIdleSkitterAt = ent.Comp.IdleSkitterUntil + ent.Comp.IdleSkitterCooldown;

        RMCNpc.SleepNPC(ent.Owner);
        TryMoveRandomly(ent.Owner, ent.Comp.IdleSkitterSpeed);
        Popup.PopupEntity(Loc.GetString("rmc-spider-skitters-madly", ("spider", ent.Owner)), ent.Owner);
    }

    private void StopAdultSpiderSkitter(Entity<RMCSpiderNestMemberComponent> ent)
    {
        if (!ent.Comp.IdleSkittering)
            return;

        ent.Comp.IdleSkittering = false;
        ent.Comp.IdleSkitterUntil = TimeSpan.Zero;
        StopMovement(ent.Owner);

        if (MobState.IsAlive(ent.Owner) && !ActorQuery.HasComp(ent.Owner))
            RMCNpc.WakeNPC(ent.Owner);
    }

    private void UpdateNurses()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCSpiderNurseComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var nurse, out var xform))
        {
            if (!MobState.IsAlive(uid))
            {
                ClearNurseWork(nurse);
                continue;
            }

            if (ActorQuery.HasComp(uid))
            {
                ClearNurseWork(nurse);
                continue;
            }

            var ent = (uid, nurse, xform);
            var mapCoords = Transform.GetMapCoordinates((uid, xform));

            if (nurse.BusyWork != RMCSpiderNurseWork.None)
            {
                UpdateNurseWork(ent, mapCoords);
                continue;
            }

            if (nurse.NextThinkAt > now)
                continue;

            nurse.NextThinkAt = now + nurse.ThinkCooldown;

            if (!Random.Prob(nurse.IdleWorkChance))
                continue;

            TryStartNurseWork(ent, mapCoords);
        }
    }

    private bool TryStartNurseWork(Entity<RMCSpiderNurseComponent, TransformComponent> ent, MapCoordinates mapCoords)
    {
        var spiders = CountNearby<RMCSpiderNestMemberComponent>(mapCoords, ent.Comp1.NestRange);
        var eggs = CountNearby<RMCSpiderEggComponent>(mapCoords, ent.Comp1.NestRange);
        var webs = CountNearby<RMCSpiderWebComponent>(mapCoords, ent.Comp1.NestRange);
        var cocoons = CountNearby<RMCSpiderCocoonComponent>(mapCoords, ent.Comp1.NestRange);

        var livingTarget = PickCocoonTarget(ent.Owner, ent.Comp1, mapCoords, true, false);
        if (livingTarget != null &&
            cocoons < ent.Comp1.MaxCocoons &&
            TryStartCocoonTarget(ent, livingTarget.Value))
            return true;

        if (webs < ent.Comp1.MaxWebs && !TileHasWeb(ent.Comp2.Coordinates))
        {
            StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.SpinWeb, null, ent.Comp1.WebSpinTime);
            return true;
        }

        if (ent.Comp1.Fed > 0 &&
            eggs < ent.Comp1.MaxEggs &&
            spiders < ent.Comp1.MaxActiveSpiders &&
            TileHasWeb(ent.Comp2.Coordinates))
        {
            StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.LayEggs, null, ent.Comp1.EggLayTime);
            return true;
        }

        var itemTarget = PickCocoonTarget(ent.Owner, ent.Comp1, mapCoords, false, true);
        return itemTarget != null &&
               cocoons < ent.Comp1.MaxCocoons &&
               TryStartCocoonTarget(ent, itemTarget.Value);
    }

    private bool TryStartCocoonTarget(Entity<RMCSpiderNurseComponent, TransformComponent> ent, EntityUid target)
    {
        if (!XformQuery.TryGetComponent(target, out var targetXform) ||
            !Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, targetXform.Coordinates, out var distance))
        {
            return false;
        }

        if (distance > ent.Comp1.CocoonRange)
        {
            StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.MovingToTarget, target, ent.Comp1.TargetGiveUpTime);
            TryMoveTowards(ent.Owner, targetXform.Coordinates, ent.Comp1.MoveToTargetSpeed);
            return true;
        }

        StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.Cocoon, target, ent.Comp1.CocoonSpinTime);
        return true;
    }

    private void UpdateNurseWork(Entity<RMCSpiderNurseComponent, TransformComponent> ent, MapCoordinates mapCoords)
    {
        if (ent.Comp1.BusyWork == RMCSpiderNurseWork.MovingToTarget)
        {
            UpdateNurseMoveToTarget(ent);
            return;
        }

        if (ent.Comp1.BusyUntil > Timing.CurTime)
            return;

        switch (ent.Comp1.BusyWork)
        {
            case RMCSpiderNurseWork.SpinWeb:
                if (CountNearby<RMCSpiderWebComponent>(mapCoords, ent.Comp1.NestRange) < ent.Comp1.MaxWebs &&
                    !TileHasWeb(ent.Comp2.Coordinates))
                {
                    Spawn(ent.Comp1.WebPrototype, ent.Comp2.Coordinates);
                    Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-finish-web", ("spider", ent.Owner)), ent.Owner);
                }
                break;
            case RMCSpiderNurseWork.LayEggs:
                if (ent.Comp1.Fed > 0 &&
                    CountNearby<RMCSpiderEggComponent>(mapCoords, ent.Comp1.NestRange) < ent.Comp1.MaxEggs &&
                    CountNearby<RMCSpiderNestMemberComponent>(mapCoords, ent.Comp1.NestRange) < ent.Comp1.MaxActiveSpiders)
                {
                    Spawn(ent.Comp1.EggPrototype, ent.Comp2.Coordinates);
                    ent.Comp1.Fed--;
                    Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-finish-eggs", ("spider", ent.Owner)), ent.Owner);
                }
                break;
            case RMCSpiderNurseWork.Cocoon:
                TryCreateCocoon(ent.Owner, ent.Comp1, mapCoords);
                break;
        }

        ClearNurseWork(ent.Comp1);
    }

    private void UpdateNurseMoveToTarget(Entity<RMCSpiderNurseComponent, TransformComponent> ent)
    {
        var target = ent.Comp1.WorkTarget;
        if (target == null ||
            TerminatingOrDeleted(target.Value) ||
            !XformQuery.TryGetComponent(target.Value, out var targetXform))
        {
            ClearNurseWork(ent.Comp1);
            return;
        }

        var targetCoords = targetXform.Coordinates;
        if (!Transform.GetMoverCoordinates(ent.Owner).TryDistance(EntityManager, targetCoords, out var distance))
        {
            ClearNurseWork(ent.Comp1);
            return;
        }

        if (distance <= ent.Comp1.CocoonRange)
        {
            StartNurseWork((ent.Owner, ent.Comp1), RMCSpiderNurseWork.Cocoon, target, ent.Comp1.CocoonSpinTime);
            return;
        }

        if (ent.Comp1.BusyUntil <= Timing.CurTime)
        {
            Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-gives-up", ("spider", ent.Owner), ("target", target.Value)), ent.Owner);
            ClearNurseWork(ent.Comp1);
            return;
        }

        TryMoveTowards(ent.Owner, targetCoords, ent.Comp1.MoveToTargetSpeed);
    }

    private void StartNurseWork(Entity<RMCSpiderNurseComponent> ent, RMCSpiderNurseWork work, EntityUid? target, TimeSpan duration)
    {
        ent.Comp.BusyWork = work;
        ent.Comp.WorkTarget = target;
        ent.Comp.BusyUntil = Timing.CurTime + duration;
        ent.Comp.TargetAcquiredAt = Timing.CurTime;

        if (work != RMCSpiderNurseWork.MovingToTarget)
            StopMovement(ent.Owner);

        switch (work)
        {
            case RMCSpiderNurseWork.SpinWeb:
                Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-start-web", ("spider", ent.Owner)), ent.Owner);
                break;
            case RMCSpiderNurseWork.LayEggs:
                Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-start-eggs", ("spider", ent.Owner)), ent.Owner);
                break;
            case RMCSpiderNurseWork.Cocoon when target != null:
                Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-start-cocoon", ("spider", ent.Owner), ("target", target.Value)), ent.Owner);
                break;
        }
    }

    private void CancelNurseWork(Entity<RMCSpiderNurseComponent> ent, bool popup)
    {
        if (ent.Comp.BusyWork == RMCSpiderNurseWork.None)
            return;

        if (popup)
            Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-work-interrupted", ("spider", ent.Owner)), ent.Owner);

        ClearNurseWork(ent.Comp);
    }

    private static void ClearNurseWork(RMCSpiderNurseComponent nurse)
    {
        nurse.BusyWork = RMCSpiderNurseWork.None;
        nurse.WorkTarget = null;
        nurse.BusyUntil = TimeSpan.Zero;
    }

    private void UpdateEggs()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCSpiderEggComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var egg, out var xform))
        {
            if (egg.HatchAt > now)
                continue;

            var mapCoords = Transform.GetMapCoordinates((uid, xform));
            var activeSpiders = CountNearby<RMCSpiderNestMemberComponent>(mapCoords, egg.NestRange);
            var remaining = Math.Max(0, egg.MaxActiveSpiders - activeSpiders);
            if (remaining <= 0)
            {
                egg.HatchAt = now + RandomTime(egg.HatchMin, egg.HatchMax);
                continue;
            }

            var amount = Math.Min(Random.Next(egg.MinSpawned, egg.MaxSpawned + 1), remaining);
            for (var i = 0; i < amount; i++)
                SpawnNear(egg.SpawnPrototype, xform.Coordinates, 1.25f);

            Popup.PopupEntity(Loc.GetString("rmc-spider-egg-hatches", ("egg", uid)), uid);
            QueueDel(uid);
        }
    }

    private void UpdateSpiderlings()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCSpiderlingGrowthComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var spiderling, out var xform))
        {
            if (ActorQuery.HasComp(uid))
                continue;

            UpdateSpiderlingSkitter((uid, spiderling, xform), now);

            if (spiderling.NoGrow || spiderling.GrowProgress <= 0f || spiderling.GrowAt > now)
                continue;

            spiderling.GrowProgress += Random.NextFloat(spiderling.GrowProgressMin, spiderling.GrowProgressMax);
            if (spiderling.GrowProgress >= spiderling.GrowProgressRequired)
            {
                Spawn(PickSpiderMaturePrototype(spiderling), xform.Coordinates);
                QueueDel(uid);
                continue;
            }

            spiderling.GrowAt = now + spiderling.GrowStepCooldown;
        }
    }

    private void UpdateSpiderlingSkitter(Entity<RMCSpiderlingGrowthComponent, TransformComponent> ent, TimeSpan now)
    {
        if (ent.Comp1.NextSkitterAt > now || !MobState.IsAlive(ent.Owner))
            return;

        ent.Comp1.NextSkitterAt = now + ent.Comp1.SkitterCooldown;

        if (Random.Prob(ent.Comp1.ChitterChance))
            Popup.PopupEntity(Loc.GetString("rmc-spiderling-chitters", ("spiderling", ent.Owner)), ent.Owner);

        if (!Random.Prob(ent.Comp1.SkitterChance))
        {
            TryMoveSpiderlingToVent(ent);
            return;
        }

        EntityUid? target = null;
        foreach (var candidate in Lookup.GetEntitiesInRange(ent.Owner, ent.Comp1.SkitterRange, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate))
        {
            if (candidate == ent.Owner || TerminatingOrDeleted(candidate))
                continue;

            target = candidate;
            break;
        }

        if (target == null || !XformQuery.TryGetComponent(target.Value, out var targetXform))
            return;

        TryMoveTowards(ent.Owner, targetXform.Coordinates, 4f);
        if (Random.Prob(0.25f))
            Popup.PopupEntity(Loc.GetString("rmc-spiderling-skitters", ("spiderling", ent.Owner)), ent.Owner);
    }

    private bool TryMoveSpiderlingToVent(Entity<RMCSpiderlingGrowthComponent, TransformComponent> ent)
    {
        if (!Random.Prob(ent.Comp1.VentSearchChance))
            return false;

        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var vent in Lookup.GetEntitiesInRange<VentEntranceComponent>(mapCoords, ent.Comp1.VentSearchRange))
        {
            if (!XformQuery.TryGetComponent(vent.Owner, out var ventXform))
                continue;

            var ventCoords = Transform.GetMapCoordinates((vent.Owner, ventXform));
            var distance = (ventCoords.Position - mapCoords.Position).Length();
            if (distance > bestDistance)
                continue;

            best = vent.Owner;
            bestDistance = distance;
        }

        if (best == null || !XformQuery.TryGetComponent(best.Value, out var targetXform))
            return false;

        TryMoveTowards(ent.Owner, targetXform.Coordinates, ent.Comp1.VentMoveSpeed);
        if (Random.Prob(0.25f))
            Popup.PopupEntity(Loc.GetString("rmc-spiderling-skitters-away", ("spiderling", ent.Owner)), ent.Owner);

        return true;
    }

    private bool TryCreateCocoon(EntityUid nurse, RMCSpiderNurseComponent comp, MapCoordinates mapCoords)
    {
        if (CountNearby<RMCSpiderCocoonComponent>(mapCoords, comp.NestRange) >= comp.MaxCocoons)
            return false;

        var target = comp.WorkTarget;
        if (target == null || !XformQuery.TryGetComponent(target.Value, out var targetXform))
            return false;

        if (!Transform.GetMoverCoordinates(nurse).TryDistance(EntityManager, targetXform.Coordinates, out var distance) ||
            distance > comp.CocoonRange)
        {
            return false;
        }

        var candidates = GetCocoonCandidates(nurse, target.Value, targetXform.Coordinates, 24);
        var valid = new List<(EntityUid Candidate, bool LivingPrey, bool LargeCocoon)>();
        var livingQueued = false;
        var largeCocoon = false;

        foreach (var candidate in candidates)
        {
            if (!CanCocoonCandidate(candidate, nurse, livingQueued, out var livingPrey, out var largeCandidate))
                continue;

            valid.Add((candidate, livingPrey, largeCandidate));
            livingQueued |= livingPrey;
            largeCocoon |= largeCandidate;
        }

        if (valid.Count == 0)
            return false;

        var cocoon = Spawn(largeCocoon ? comp.LargeCocoonPrototype : comp.CocoonPrototype, targetXform.Coordinates);
        if (!TryComp<RMCSpiderCocoonComponent>(cocoon, out var cocoonComp))
            return false;

        var container = Container.EnsureContainer<Container>(cocoon, cocoonComp.ContainerId);
        var inserted = 0;
        var fed = false;

        foreach (var candidate in valid)
        {
            if (container.ContainedEntities.Count >= cocoonComp.MaxContents)
                break;

            if (!Container.Insert((candidate.Candidate, null, null, null), container, force: true))
                continue;

            inserted++;
            fed |= candidate.LivingPrey;
        }

        if (inserted == 0)
        {
            QueueDel(cocoon);
            return false;
        }

        if (fed)
        {
            comp.Fed++;
            Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-cocoon-fed", ("spider", nurse), ("target", target.Value)), nurse);
        }

        Popup.PopupEntity(Loc.GetString("rmc-spider-nurse-cocoon-created", ("spider", nurse), ("cocoon", cocoon)), nurse);

        return true;
    }

    private List<EntityUid> GetCocoonCandidates(EntityUid nurse, EntityUid target, EntityCoordinates coordinates, int maxContents)
    {
        var candidates = new List<EntityUid>(maxContents);
        var seen = new HashSet<EntityUid>();

        AddCandidate(target);

        foreach (var candidate in Lookup.GetEntitiesInRange(coordinates, 0.35f, LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Approximate))
        {
            if (candidates.Count >= maxContents)
                break;

            AddCandidate(candidate);
        }

        return candidates;

        void AddCandidate(EntityUid candidate)
        {
            if (candidate == nurse ||
                !seen.Add(candidate) ||
                TerminatingOrDeleted(candidate))
            {
                return;
            }

            candidates.Add(candidate);
        }
    }

    private bool CanCocoonCandidate(EntityUid candidate, EntityUid nurse, bool alreadyFed, out bool livingPrey, out bool largeCocoon)
    {
        livingPrey = false;
        largeCocoon = false;

        if (candidate == nurse ||
            HasComp<RMCSpiderNestMemberComponent>(candidate) ||
            HasComp<RMCSpiderCocoonComponent>(candidate) ||
            TerminatingOrDeleted(candidate) ||
            !XformQuery.TryGetComponent(candidate, out var xform) ||
            xform.Anchored)
        {
            return false;
        }

        if (MobQuery.TryComp(candidate, out var mob))
        {
            if (alreadyFed || !MobState.IsIncapacitated(candidate, mob))
                return false;

            livingPrey = true;
            largeCocoon = true;
            return true;
        }

        if (ItemQuery.HasComp(candidate))
            return true;

        largeCocoon = DamageableQuery.HasComp(candidate);
        return largeCocoon;
    }

    private EntityUid? PickCocoonTarget(EntityUid nurse, RMCSpiderNurseComponent comp, MapCoordinates mapCoords, bool includeLiving, bool includeItems)
    {
        if (includeLiving)
        {
            foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, comp.TargetSearchRange))
            {
                if (mob.Owner == nurse ||
                    HasComp<RMCSpiderNestMemberComponent>(mob.Owner) ||
                    !MobState.IsIncapacitated(mob.Owner, mob.Comp))
                {
                    continue;
                }

                comp.TargetAcquiredAt = Timing.CurTime;
                return mob.Owner;
            }
        }

        if (includeItems)
        {
            foreach (var item in Lookup.GetEntitiesInRange<ItemComponent>(mapCoords, comp.TargetSearchRange))
            {
                if (!CanCocoonCandidate(item.Owner, nurse, false, out _, out _))
                    continue;

                comp.TargetAcquiredAt = Timing.CurTime;
                return item.Owner;
            }

            foreach (var damageable in Lookup.GetEntitiesInRange<DamageableComponent>(mapCoords, comp.TargetSearchRange))
            {
                if (ItemQuery.HasComp(damageable.Owner) ||
                    MobQuery.HasComp(damageable.Owner) ||
                    !CanCocoonCandidate(damageable.Owner, nurse, false, out _, out _))
                {
                    continue;
                }

                comp.TargetAcquiredAt = Timing.CurTime;
                return damageable.Owner;
            }
        }

        return null;
    }

    private bool TileHasWeb(EntityCoordinates coords)
    {
        foreach (var web in Lookup.GetEntitiesInRange<RMCSpiderWebComponent>(coords, 0.3f))
        {
            if (web.Owner.Valid)
                return true;
        }

        return false;
    }

    private EntProtoId PickSpiderMaturePrototype(RMCSpiderlingGrowthComponent comp)
    {
        var total = comp.GuardWeight + comp.HunterWeight + comp.NurseWeight;
        var roll = Random.NextFloat(0f, total);

        if (roll < comp.GuardWeight)
            return comp.GuardPrototype;

        if (roll < comp.GuardWeight + comp.HunterWeight)
            return comp.HunterPrototype;

        return comp.NursePrototype;
    }
}
