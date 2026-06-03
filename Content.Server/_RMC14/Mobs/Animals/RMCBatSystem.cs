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

public sealed class RMCBatSystem : RMCAnimalSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBatHangingComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<RMCBatHangingComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<RMCBatHangingComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextCheckAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.CheckCooldown);
    }

    private void OnDamageChanged(Entity<RMCBatHangingComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased)
            WakeBat(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        var query = EntityQueryEnumerator<RMCBatHangingComponent>();
        while (query.MoveNext(out var uid, out var bat))
        {
            if (bat.NextCheckAt > now)
                continue;

            bat.NextCheckAt = now + bat.CheckCooldown;

            if (ActorQuery.HasComp(uid) || !MobState.IsAlive(uid))
            {
                WakeBat((uid, bat));
                continue;
            }

            if (bat.Hanging)
            {
                if (Random.Prob(bat.WakeChance) || TryWakeFromDisturbance((uid, bat)))
                    WakeBat((uid, bat));
            }
            else if (Random.Prob(bat.HangChance) && CanHang(uid, bat))
            {
                HangBat((uid, bat));
            }
        }
    }

    private bool TryWakeFromDisturbance(Entity<RMCBatHangingComponent> ent)
    {
        if (ent.Comp.DisturbanceRange <= 0)
            return false;

        var coords = Transform.GetMapCoordinates(ent.Owner);
        foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(coords, ent.Comp.DisturbanceRange))
        {
            if (mob.Owner == ent.Owner ||
                HasComp<RMCBatHangingComponent>(mob.Owner) ||
                !MobState.IsAlive(mob.Owner, mob.Comp))
            {
                continue;
            }

            if (!Random.Prob(ent.Comp.DisturbanceWakeChance))
                return false;

            Popup.PopupEntity(Loc.GetString("rmc-bat-wakes-disturbed", ("bat", ent.Owner)), ent.Owner);
            return true;
        }

        return false;
    }

    private bool CanHang(EntityUid uid, RMCBatHangingComponent comp)
    {
        if (!comp.RequireBlockedNorth)
            return true;

        var coords = Transform(uid).Coordinates.Offset(Direction.North.ToVec());
        return _turf.TryGetTileRef(coords, out var tile) &&
               _turf.IsTileBlocked(tile.Value, CollisionGroup.Impassable);
    }

    private void HangBat(Entity<RMCBatHangingComponent> ent)
    {
        ent.Comp.Hanging = true;
        StopMovement(ent.Owner);
        _appearance.SetData(ent.Owner, RMCBatVisuals.Hanging, true);
        RMCNpc.SleepNPC(ent.Owner);
    }

    private void WakeBat(Entity<RMCBatHangingComponent> ent)
    {
        if (!ent.Comp.Hanging)
            return;

        ent.Comp.Hanging = false;
        _appearance.SetData(ent.Owner, RMCBatVisuals.Hanging, false);
        RMCNpc.WakeNPC(ent.Owner);
    }
}
