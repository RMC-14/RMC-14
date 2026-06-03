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

public sealed class RMCFarmAnimalSystem : RMCAnimalSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCGoatTemperComponent, MapInitEvent>(OnGoatMapInit);
        SubscribeLocalEvent<RMCCowTippableComponent, DisarmedEvent>(OnCowDisarmed);
        SubscribeLocalEvent<RMCFarmAnimalEmoteComponent, MapInitEvent>(OnFarmEmoteMapInit);
        SubscribeLocalEvent<RMCChickenFedEggLayerComponent, MapInitEvent>(OnChickenMapInit);
        SubscribeLocalEvent<RMCChickenFedEggLayerComponent, InteractUsingEvent>(OnChickenInteractUsing);
        SubscribeLocalEvent<RMCChickenEggHatchComponent, MapInitEvent>(OnChickenEggMapInit);
        SubscribeLocalEvent<RMCChickGrowthComponent, MapInitEvent>(OnChickMapInit);
    }

    private void OnGoatMapInit(Entity<RMCGoatTemperComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
    }

    private void OnCowDisarmed(Entity<RMCCowTippableComponent> ent, ref DisarmedEvent args)
    {
        if (args.Handled ||
            args.Target != ent.Owner ||
            !MobState.IsAlive(ent.Owner) ||
            ent.Comp.NextTipAt > Timing.CurTime)
        {
            return;
        }

        ent.Comp.NextTipAt = Timing.CurTime + ent.Comp.TipCooldown;
        ent.Comp.TippedUntil = Timing.CurTime + ent.Comp.TipTime;
        Stun.TryKnockdown(ent.Owner, ent.Comp.TipTime, true);
        Popup.PopupEntity(Loc.GetString("rmc-cow-tipped-user", ("cow", ent.Owner)), ent.Owner, args.Source);
        Popup.PopupEntity(Loc.GetString("rmc-cow-tipped-others", ("cow", ent.Owner), ("user", args.Source)), ent.Owner);
        args.IsStunned = true;
        args.Handled = true;
    }

    private void OnFarmEmoteMapInit(Entity<RMCFarmAnimalEmoteComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextEmoteAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.EmoteCooldownMax);
    }

    private void OnChickenMapInit(Entity<RMCChickenFedEggLayerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextLayCheckAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.LayCheckCooldown);
    }

    private void OnChickenEggMapInit(Entity<RMCChickenEggHatchComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.HatchAt = Timing.CurTime + RandomTime(ent.Comp.HatchMin, ent.Comp.HatchMax);
    }

    private void OnChickMapInit(Entity<RMCChickGrowthComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.GrowAt = Timing.CurTime + RandomTime(ent.Comp.GrowMin, ent.Comp.GrowMax);
    }

    private void OnChickenInteractUsing(Entity<RMCChickenFedEggLayerComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled ||
            !MobState.IsAlive(ent.Owner) ||
            !Tags.HasTag(args.Used, ent.Comp.FeedTag))
        {
            return;
        }

        if (ent.Comp.EggCredits >= ent.Comp.MaxEggCredits)
        {
            Popup.PopupEntity(Loc.GetString("rmc-chicken-not-hungry", ("chicken", ent.Owner)), ent.Owner, args.User);
            args.Handled = true;
            return;
        }

        var added = Random.Next(ent.Comp.MinFeedCredits, ent.Comp.MaxFeedCredits + 1);
        ent.Comp.EggCredits = Math.Min(ent.Comp.MaxEggCredits, ent.Comp.EggCredits + added);
        QueueDel(args.Used);
        Popup.PopupEntity(Loc.GetString("rmc-chicken-fed", ("chicken", ent.Owner)), ent.Owner, args.User);
        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateGoats();
        UpdateFarmAnimalEmotes();
        UpdateChickenEggs();
        UpdateChicks();
        UpdateChickens();
    }

    private void UpdateGoats()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCGoatTemperComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var goat, out var xform))
        {
            if (goat.NextThinkAt > now)
                continue;

            goat.NextThinkAt = now + goat.ThinkCooldown;
            if (!MobState.IsAlive(uid))
                continue;

            var hostiles = Faction.GetHostiles(uid).ToArray();
            if (hostiles.Length > 0)
            {
                if (!Random.Prob(goat.CalmChance))
                    continue;

                foreach (var hostile in hostiles)
                    Faction.DeAggroEntity(uid, hostile);

                Popup.PopupEntity(Loc.GetString("rmc-goat-calms", ("goat", uid)), uid);
                continue;
            }

            if (!Random.Prob(goat.MadChance))
                continue;

            var candidates = new List<EntityUid>();
            var mapCoords = Transform.GetMapCoordinates((uid, xform));
            foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, goat.SearchRange))
            {
                if (mob.Owner == uid ||
                    HasComp<RMCGoatTemperComponent>(mob.Owner) ||
                    !MobState.IsAlive(mob.Owner, mob.Comp))
                {
                    continue;
                }

                candidates.Add(mob.Owner);
            }

            if (candidates.Count == 0)
                continue;

            var target = Random.Pick(candidates);
            Faction.AggroEntity(uid, target);
            Popup.PopupEntity(Loc.GetString("rmc-goat-evil-gleam", ("goat", uid)), uid);
        }
    }

    private void UpdateFarmAnimalEmotes()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCFarmAnimalEmoteComponent>();
        while (query.MoveNext(out var uid, out var emote))
        {
            if (emote.NextEmoteAt > now)
                continue;

            emote.NextEmoteAt = now + RandomTime(emote.EmoteCooldownMin, emote.EmoteCooldownMax);
            if (ActorQuery.HasComp(uid) || !MobState.IsAlive(uid) || emote.Emotes.Count == 0 || !Random.Prob(emote.EmoteChance))
                continue;

            Popup.PopupEntity(Loc.GetString(Random.Pick(emote.Emotes), ("animal", uid)), uid);
        }
    }

    private void UpdateChickenEggs()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCChickenEggHatchComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var egg, out var xform))
        {
            if (egg.HatchAt > now)
                continue;

            if (Container.IsEntityInContainer(uid))
            {
                egg.HatchAt = now + RandomTime(egg.HatchMin, egg.HatchMax);
                continue;
            }

            Spawn(egg.SpawnPrototype, xform.Coordinates);
            Popup.PopupEntity(Loc.GetString("rmc-chicken-egg-hatches", ("egg", uid)), uid);
            QueueDel(uid);
        }
    }

    private void UpdateChicks()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCChickGrowthComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var chick, out var xform))
        {
            if (chick.GrowAt > now || !MobState.IsAlive(uid))
                continue;

            Spawn(Random.Pick(chick.MaturePrototypes), xform.Coordinates);
            QueueDel(uid);
        }
    }

    private void UpdateChickens()
    {
        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCChickenFedEggLayerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var chicken, out var xform))
        {
            if (chicken.NextLayCheckAt > now)
                continue;

            chicken.NextLayCheckAt = now + chicken.LayCheckCooldown;

            if (!MobState.IsAlive(uid) ||
                chicken.EggCredits <= 0 ||
                !Random.Prob(chicken.LayChance))
            {
                continue;
            }

            var egg = chicken.EggPrototype;
            var mapCoords = Transform.GetMapCoordinates((uid, xform));
            if (Random.Prob(chicken.FertilizedEggChance) &&
                CountNearby<RMCChickenComponent>(mapCoords, chicken.ChickenCapRange) < chicken.MaxNearbyChickens)
            {
                egg = chicken.FertilizedEggPrototype;
            }

            Spawn(egg, xform.Coordinates);
            chicken.EggCredits--;
            Popup.PopupEntity(Loc.GetString("rmc-chicken-lays-egg", ("chicken", uid)), uid);
        }
    }
}
