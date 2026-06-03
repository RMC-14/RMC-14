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

public sealed class RMCCarpSystem : RMCAnimalSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCCarpComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCCarpComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnMapInit(Entity<RMCCarpComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextGnashAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.GnashCooldownMax);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCCarpComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var carp, out var xform))
        {
            if (carp.NextGnashAt > now || !MobState.IsAlive(uid) || ActorQuery.HasComp(uid))
                continue;

            carp.NextGnashAt = now + RandomTime(carp.GnashCooldownMin, carp.GnashCooldownMax);
            TryGnashAtTarget((uid, carp, xform));
        }
    }

    private void OnMeleeHit(Entity<RMCCarpComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.User != ent.Owner)
            return;

        foreach (var target in args.HitEntities)
        {
            if (!ValidLivingMob(target) || !Random.Prob(ent.Comp.KnockdownChance))
                continue;

            Stun.TryKnockdown(target, ent.Comp.KnockdownTime, true);
            Popup.PopupEntity(Loc.GetString("rmc-carp-knocks-down", ("carp", ent.Owner), ("target", target)), target, PopupType.MediumCaution);
        }
    }

    private void TryGnashAtTarget(Entity<RMCCarpComponent, TransformComponent> ent)
    {
        var mapCoords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        EntityUid? best = null;
        var bestDistance = float.MaxValue;

        foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(mapCoords, ent.Comp1.TargetSearchRange))
        {
            if (mob.Owner == ent.Owner ||
                !MobState.IsAlive(mob.Owner, mob.Comp) ||
                Faction.IsEntityFriendly(ent.Owner, mob.Owner))
            {
                continue;
            }

            var targetCoords = Transform.GetMapCoordinates(mob.Owner);
            var distance = (targetCoords.Position - mapCoords.Position).LengthSquared();
            if (distance >= bestDistance)
                continue;

            best = mob.Owner;
            bestDistance = distance;
        }

        if (best is { } target)
            Popup.PopupEntity(Loc.GetString("rmc-carp-gnashes", ("carp", ent.Owner), ("target", target)), ent.Owner);
    }
}
