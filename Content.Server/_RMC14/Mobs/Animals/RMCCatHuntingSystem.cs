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

public sealed class RMCCatHuntingSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCCatHunterComponent, MapInitEvent>(OnCatMapInit);
        SubscribeLocalEvent<RMCCatHunterComponent, ComponentShutdown>(OnCatShutdown);
    }

    private void OnCatMapInit(Entity<RMCCatHunterComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
        ent.Comp.NextMeowAt = Timing.CurTime + RandomTime(ent.Comp.MeowCooldownMin, ent.Comp.MeowCooldownMax);
        ent.Comp.NextAmbientEmoteAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.AmbientEmoteCooldown);
    }

    private void OnCatShutdown(Entity<RMCCatHunterComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.MovementTarget = null;
        ent.Comp.PlayCounter = 0;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCCatHunterComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var hunter, out var xform))
        {
            if (!MobState.IsAlive(uid))
            {
                hunter.MovementTarget = null;
                hunter.PlayCounter = 0;
                continue;
            }

            TryMeow((uid, hunter), now);
            TryAmbientCatEmote((uid, hunter), now);

            if (ActorQuery.HasComp(uid) || hunter.NextThinkAt > now)
                continue;

            hunter.NextThinkAt = now + hunter.ThinkCooldown;

            var prey = PickPrey((uid, hunter, xform));
            if (prey == null)
            {
                hunter.MovementTarget = null;
                hunter.PlayCounter = 0;
                continue;
            }

            var preyCoords = Transform.GetMoverCoordinates(prey.Value);
            if (!Transform.GetMoverCoordinates(uid).TryDistance(EntityManager, preyCoords, out var distance))
                continue;

            if (hunter.MovementTarget != prey.Value)
            {
                hunter.MovementTarget = prey;
                hunter.PlayCounter = 0;
                Popup.PopupEntity(Loc.GetString("rmc-cat-pounces-at", ("cat", uid), ("prey", prey.Value)), uid);
            }

            TryThreatenPrey(uid, prey.Value, hunter, distance, now);

            if (distance > hunter.AttackRange)
            {
                TryMoveTowards(uid, preyCoords, hunter.MoveSpeed);
                continue;
            }

            if (hunter.PlayCounter >= hunter.MaxPlayAttacks)
            {
                hunter.MovementTarget = null;
                hunter.PlayCounter = 0;
                hunter.NextThinkAt = now + hunter.PlayBreakCooldown;
                continue;
            }

            AttackPrey(uid, prey.Value, hunter);
        }
    }

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

    private void TryMeow(Entity<RMCCatHunterComponent> ent, TimeSpan now)
    {
        if (ent.Comp.NextMeowAt > now)
            return;

        ent.Comp.NextMeowAt = now + RandomTime(ent.Comp.MeowCooldownMin, ent.Comp.MeowCooldownMax);
        _audio.PlayPvs(ent.Comp.MeowSound, ent.Owner);
    }

    private void TryAmbientCatEmote(Entity<RMCCatHunterComponent> ent, TimeSpan now)
    {
        if (ActorQuery.HasComp(ent.Owner) ||
            ent.Comp.NextAmbientEmoteAt > now)
        {
            return;
        }

        if (Random.Prob(ent.Comp.HeardEmoteChance))
        {
            ent.Comp.NextAmbientEmoteAt = now + ent.Comp.AmbientEmoteCooldown;
            Popup.PopupEntity(Loc.GetString(PickCatHeardEmote(), ("cat", ent.Owner)), ent.Owner);
            return;
        }

        if (!Random.Prob(ent.Comp.SeenEmoteChance))
            return;

        ent.Comp.NextAmbientEmoteAt = now + ent.Comp.AmbientEmoteCooldown;
        Popup.PopupEntity(Loc.GetString(PickCatSeenEmote(), ("cat", ent.Owner)), ent.Owner);
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

    private string PickCatHeardEmote()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-cat-meows",
            1 => "rmc-cat-mews",
            _ => "rmc-cat-mrrps",
        };
    }

    private string PickCatSeenEmote()
    {
        return Random.Next(3) switch
        {
            0 => "rmc-cat-shakes-head",
            1 => "rmc-cat-shivers",
            _ => "rmc-cat-licks-paw",
        };
    }
}
